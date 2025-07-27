using ClassicUO.Network.Encryption;
using ClassicUO.Utility.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using SDL2;

namespace ClassicUO.Network
{
    sealed class AsyncSocketWrapper : IDisposable
    {
        private TcpClient _socket;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;
        public bool IsConnected => _socket?.Client?.Connected ?? false;
        public EndPoint LocalEndPoint => _socket?.Client?.LocalEndPoint;

        public event EventHandler OnConnected, OnDisconnected;
        public event EventHandler<SocketError> OnError;
        public event EventHandler<byte[]> OnDataReceived;

        public async Task<bool> ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
                return true;

            try
            {
                _socket = new TcpClient();
                _socket.NoDelay = true;
                _cancellationTokenSource = new CancellationTokenSource();

                await _socket.ConnectAsync(ip, port);

                if (!IsConnected)
                {
                    OnError?.Invoke(this, SocketError.NotConnected);

                    return false;
                }

                _stream = _socket.GetStream();

                // Start background receive task
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                OnConnected?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (SocketException socketEx)
            {
                Log.Error($"Error while connecting {socketEx}");
                OnError?.Invoke(this, socketEx.SocketErrorCode);

                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Error while connecting {ex}");
                OnError?.Invoke(this, SocketError.SocketError);

                return false;
            }
        }

        public async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
                return;

            try
            {
                await _stream.WriteAsync(buffer, offset, count, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while sending {ex}");
                OnError?.Invoke(this, SocketError.SocketError);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    int toRead = Math.Min(buffer.Length, _socket.Client.Available);
                    int bytesRead = -1;
                    if(toRead > 0)
                        bytesRead = await _stream.ReadAsync(buffer, 0, toRead, cancellationToken);

                    if (bytesRead == 0)
                    {
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                        Disconnect();

                        break;
                    }

                    if (bytesRead > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        var data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);
                        OnDataReceived?.Invoke(this, data);
                    }

                    await Task.Delay(1, cancellationToken);
                }
            }
            catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx)
            {
                Disconnect();

                switch (socketEx.SocketErrorCode)
                {
                    case SocketError.OperationAborted: OnError?.Invoke(this, SocketError.Success); break;
                    default: 
                        Log.Error($"Socket error in receive loop: {socketEx.SocketErrorCode} - {socketEx.Message}");
                        OnError?.Invoke(this, socketEx.SocketErrorCode); break;
                }

            }
            catch (OperationAbortedException)
            {
                Disconnect();
                OnError?.Invoke(this, SocketError.Success);
            }
            catch (OperationCanceledException)
            {
                Disconnect();
                OnError?.Invoke(this, SocketError.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in receive loop {ex}");
                Disconnect();
                OnError?.Invoke(this, SocketError.SocketError);
            }
        }

        private bool _isDisconnecting;
        public void Disconnect()
        {
            if (_isDisconnecting)
                return;

            _isDisconnecting = true;
            
            _cancellationTokenSource?.Cancel();
            _receiveTask?.Wait(5000);
            _stream?.Close();
            _socket?.Close();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _receiveTask?.Wait(5000);
            _stream?.Dispose();
            _socket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }

    internal sealed class AsyncNetClient : IDisposable
    {
        private const int BUFF_SIZE = 0x10000;

        private readonly byte[] _compressedBuffer = new byte[4096];
        private readonly byte[] _uncompressedBuffer = new byte[BUFF_SIZE];
        private readonly Huffman _huffman = new Huffman();
        private bool _isCompressionEnabled;
        private readonly AsyncSocketWrapper _socket;
        private uint? _localIP;
        private readonly CircularBuffer _sendStream;
        private readonly ConcurrentQueue<byte[]> _incomingMessages = new();
        private Task _networkTask;
        private CancellationTokenSource _cancellationTokenSource = new();

        public AsyncNetClient()
        {
            Statistics = new NetStatistics(this);
            _sendStream = new CircularBuffer();

            _socket = new AsyncSocketWrapper();

            _socket.OnConnected += (o, e) =>
            {
                Statistics.Reset();
                Connected?.Invoke(this, EventArgs.Empty);
            };

            _socket.OnDisconnected += (o, e) => Disconnected?.Invoke(this, SocketError.Success);
            _socket.OnError += (o, e) => Disconnected?.Invoke(this, e);
            _socket.OnDataReceived += OnDataReceived;
        }

        public static AsyncNetClient Socket { get; set; } = new AsyncNetClient();

        public bool IsConnected => _socket != null && _socket.IsConnected;
        public NetStatistics Statistics { get; }

        public uint LocalIP
        {
            get
            {
                if (!_localIP.HasValue)
                {
                    try
                    {
                        byte[] addressBytes = (_socket?.LocalEndPoint as IPEndPoint)?.Address.MapToIPv4().GetAddressBytes();

                        if (addressBytes != null && addressBytes.Length != 0)
                        {
                            _localIP = (uint)(addressBytes[3] | (addressBytes[2] << 8) | (addressBytes[1] << 16) | (addressBytes[0] << 24));
                        }

                        if (!_localIP.HasValue || _localIP == 0)
                        {
                            _localIP = 0x100007f;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"error while retrieving local endpoint address: \n{ex}");
                        _localIP = 0x100007f;
                    }
                }

                return _localIP.Value;
            }
        }

        public event EventHandler Connected;
        public event EventHandler<SocketError> Disconnected;
        public static event EventHandler<byte[]> MessageReceived;

        public async Task<bool> Connect(string ip, ushort port, CancellationToken cancellationToken = new ())
        {
            _sendStream.Clear();
            _huffman.Reset();
            Statistics.Reset();

            var success = await _socket.ConnectAsync(ip, port, cancellationToken);

            if (success)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _networkTask = Task.Run(() => NetworkLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            return success;
        }

        private bool _isDisconnecting;
        public async Task Disconnect()
        {
            if (_isDisconnecting)
                return;

            _isDisconnecting = true;
            
            SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_FALSE);
            _isCompressionEnabled = false;
            Statistics.Reset();

            _cancellationTokenSource?.Cancel();

            if(_networkTask != null)
            {
                try
                {
                    await Task.WhenAny(_networkTask,Task.Delay(5000));
                }
                catch { }
            }

            _socket.Disconnect();
            _huffman.Reset();
            _sendStream.Clear();
        }

        public void EnableCompression()
        {
            _isCompressionEnabled = true;
            _huffman.Reset();
            _sendStream.Clear();
        }

        private async Task NetworkLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    // Process outgoing data
                    ProcessSendAsync(cancellationToken);

                    // Update statistics
                    Statistics.Update();

                    // Small delay to prevent excessive CPU usage
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await Disconnect();
                    Disconnected?.Invoke(this, SocketError.Success);
                    break;
                }
                catch (Exception ex)
                {
                    await Disconnect();
                    Log.Error($"Network loop error: {ex}");
                    Disconnected?.Invoke(this, SocketError.SocketError);
                    break;
                }
            }
        }

        private void OnDataReceived(object sender, byte[] data)
        {
            try
            {
                Statistics.TotalBytesReceived += (uint)data.Length;

                var span = data.AsSpan();
                ProcessEncryption(span);
                var decompressed = DecompressBuffer(span);

                if (!decompressed.IsEmpty)
                {
                    var message = decompressed.ToArray();
                    _incomingMessages.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing received data: {ex}");
            }
        }

        public void ProcessIncomingMessages()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                while (_incomingMessages.TryDequeue(out _))
                {
                }

                return;
            }
            
            while (_incomingMessages.TryDequeue(out var message))
            {
                MessageReceived?.Invoke(this, message);
            }
        }
        
        public void Send(Span<byte> message, bool ignorePlugin = false, bool skipEncryption = false)
        {
            if (!IsConnected || message == null || message.Length == 0)
            {
                return;
            }
            
            if (!ignorePlugin && !Plugin.ProcessSendPacket(ref message))
            {
                return;
            }

            if (message.IsEmpty)
                return;

            PacketLogger.Default?.Log(message, true);

            if (!skipEncryption)
            {
                EncryptionHelper.Encrypt(!_isCompressionEnabled, message, message, message.Length);
            }

            lock (_sendStream)
            {
                _sendStream.Enqueue(message);
            }

            Statistics.TotalBytesSent += (uint)message.Length;
            Statistics.TotalPacketsSent++;
        }

        private void ProcessEncryption(Span<byte> buffer)
        {
            if (!_isCompressionEnabled)
                return;

            EncryptionHelper.Decrypt(buffer, buffer, buffer.Length);
        }

        //Not really async, but sends data async inside
        private void ProcessSendAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected)
                return;

            try
            {
                lock (_sendStream)
                {
                    if (_sendStream.Length > 0)
                    {
                        var sendingBuffer = new byte[4096];
                        
                        int size = Math.Max(sendingBuffer.Length, _sendStream.Length);
                        
                        var read = _sendStream.Dequeue(sendingBuffer, 0, size);

                        if (read > 0)
                        {
                            _socket.SendAsync(sendingBuffer, 0, read, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ProcessSendAsync: {ex}");
                Disconnected?.Invoke(this, SocketError.SocketError);
            }
        }

        private Span<byte> DecompressBuffer(Span<byte> buffer)
        {
            if (!_isCompressionEnabled)
                return buffer;

            var size = 65536;

            if (!_huffman.Decompress(buffer, _uncompressedBuffer, ref size))
            {
                _ = Disconnect();
                Disconnected?.Invoke(this, SocketError.SocketError);

                return Span<byte>.Empty;
            }

            return _uncompressedBuffer.AsSpan(0, size);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _networkTask?.Wait(5000);
            }
            catch { }
            _socket?.Dispose();
        }
    }
}