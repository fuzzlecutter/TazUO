#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Resources;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using System;

namespace ClassicUO.Game.Data
{
    public enum Lock : byte
    {
        Up = 0,
        Down = 1,
        Locked = 2
    }

    public sealed class Skill
    {
        public static event EventHandler<SkillChangeArgs> SkillValueChangedEvent;
        public static event EventHandler<SkillChangeArgs> SkillBaseChangedEvent;
        public static event EventHandler<SkillChangeArgs> SkillCapChangedEvent;

        public Skill(string name, int index, bool click)
        {
            Name = name;
            Index = index;
            IsClickable = click;
        }

        public Lock? Lock { get; internal set; }

        public ushort ValueFixed { get; internal set; }

        public ushort? BaseFixed { get; internal set; }

        public ushort CapFixed { get; internal set; }

        public float Value => ValueFixed / 10.0f;

        public float Base
        {
            get
            {
                if (BaseFixed.HasValue)
                {
                    return BaseFixed.Value / 10.0f;
                }

                if (World.Player is not PlayerMobile player)
                {
                    return 0f;
                }

                return ClassicSkillCalculator.ComputeRealSkill(Index, ValueFixed, player.Strength, player.Dexterity, player.Intelligence) / 10f;
            }
        }

        public float Cap => CapFixed / 10.0f;

        public bool IsClickable { get; }

        public string Name { get; }

        public int Index { get; }

        public static void InvokeSkillValueChanged(int index)
        {
            SkillValueChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        }
        public static void InvokeSkillBaseChanged(int index)
        {
            SkillBaseChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        }
        public static void InvokeSkillCapChanged(int index)
        {
            SkillCapChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        }

        public override string ToString()
        {
            return string.Format(ResGeneral.Name0Val1, Name, Value);
        }

        public class SkillChangeArgs : EventArgs
        {
            public int Index;
            public SkillChangeArgs(int index)
            {
                Index = index;
            }
        }
    }

    static class ClassicSkillCalculator
    {
        // https://web.archive.org/web/19981203001926fw_/http://uoss.stratics.com/skillscalc.htm
        static readonly (float str, float dex, float intel)[] Skills = new (float, float, float)[]
        {
            (   0f,  0.5f,  0.5f), // Alchemy
            (   0f,    0f,    0f), // Anatomy
            (   0f,    0f,    0f), // Animal Lore
            (   0f,    0f,    0f), // Item Identification
            (   0f,    0f,    0f), // Arms Lore
            (0.75f, 0.25f,    0f), // Parrying
            (   0f,    0f,    0f), // Begging
            ( 0.1f,    0f,    0f), // Blacksmithy
            (   1f,    1f,    0f), // Bowcraft/Fletching
            (   0f,    0f,    0f), // Peacemaking
            (   2f,  1.5f,  1.5f), // Camping
            (   2f,  0.5f,    0f), // Carpentry
            (   0f, 0.75f, 0.75f), // Cartography
            (   0f,    2f,    3f), // Cooking
            (   0f,    0f,    0f), // Detecting Hidden
            (   0f, 0.25f, 0.25f), // Enticement
            (   0f,    0f,    0f), // Evaluating Intelligence
            ( 0.6f,  0.6f,  0.8f), // Healing
            (   0f,    0f,    0f), // Fishing
            (   0f,    0f,    0f), // Forensic Evaluation
            ( 1.6f, 0.65f, 0.25f), // Herding
            (   0f,    0f,    0f), // Hiding
            (   0f, 0.45f, 0.05f), // Provocation
            (   0f,  0.2f,  0.8f), // Inscription
            (   0f,  2.5f,    0f), // Lockpicking
            (   0f,    0f,  1.5f), // Magery
            (   0f,    0f,  1.5f), // Resisting Spells
            (0.75f, 0.25f,    0f), // Tactics
            (   0f,  2.5f,    0f), // Snooping
            (   0f,    0f,    0f), // Musicianship
            (   0f,  0.4f,  1.6f), // Poisoning
            (0.55f, 0.45f,    0f), // Archery
            (   0f,    0f,    0f), // Spirit Speak
            (   0f,    1f,    0f), // Stealing
            ( 0.4f,  1.6f,  0.5f), // Tailoring
            ( 1.4f,  0.2f,  0.4f), // Animal Taming
            (   0f,    0f,    0f), // Taste Identification
            ( 0.5f,  0.2f,  0.3f), // Tinkering
            (   0f, 1.25f, 1.25f), // Tracking
            ( 0.8f,  0.4f,  0.8f), // Veterinary
            (0.75f, 0.25f,    0f), // Swordsmanship
            (   1f,    0f,    0f), // Mace Fighting
            ( 0.5f,  0.5f,    0f), // Fencing
            (   1f,    0f,    0f), // Wrestling
            (   2f,    0f,    0f), // Lumberjacking
            (   2f,    0f,    0f), // Mining
        };

        internal static int ComputeRealSkill(int skillIndex, int totalSkill, int str, int dex, int intel)
        {
            if (skillIndex < 0 || skillIndex >= Skills.Length)
            {
                return 0;
            }

            str = Clamp(str, 0, 100);
            dex = Clamp(dex, 0, 100);
            intel = Clamp(intel, 0, 100);

            var weights = Skills[skillIndex];
            float statBenefit = weights.str * str + weights.dex * dex + weights.intel * intel;
            float realSkillRatio = (totalSkill - statBenefit) / (1000 - statBenefit);
            return Clamp((int)Math.Round(realSkillRatio * 1000, MidpointRounding.AwayFromZero), 0, 1000);
        }

        static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}