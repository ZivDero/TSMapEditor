using System.Collections.Generic;
using System.Collections.Immutable;
using TSMapEditor.Extensions;

namespace TSMapEditor.Models
{
    public struct Sound
    {
        public Sound(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Index} {Name}";
        }

        public int Index;
        public string Name;
    }

    public class Sounds
    {
        public Sounds(IniFileEx soundIni)
        {
            Initialize(soundIni);
        }

        public ImmutableList<Sound> List { get; private set; }

        private void Initialize(IniFileEx evaIni)
        {
            var sounds = new List<Sound>();

            const string speechSectionName = "SoundList";

            evaIni.DoForEveryValueInSection(speechSectionName, name =>
            {
                if (string.IsNullOrEmpty(name))
                    return;

                sounds.Add(new Sound(sounds.Count, name));
            });

            List = ImmutableList.Create(sounds.ToArray());
        }
    }
}