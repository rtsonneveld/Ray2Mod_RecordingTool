using Ray2Mod.Game;
using Ray2Mod.Game.Structs.Input;

namespace Ray2Mod_RecordingTool {

    public class FrameData {
        public long frameNumber;
        public int[] ValidCounts;
        public byte globalRandomizer;

        public FrameData(long frameNumber)
        {
            this.frameNumber = frameNumber;
        }

        public unsafe FrameData Record()
        {
            InputStructure* iptStructure = RecordingTool.World.InputStructure;

            var actions = iptStructure->EntryActions;
            int[] validCounts = new int[actions.Length];
            for (int i=0;i<actions.Length;i++) {
                validCounts[i] = actions[i]->validCount;
            }

            ValidCounts = validCounts;

            globalRandomizer = RecordingTool.World.GlobalRandomizer;

            return this;
        }

        public unsafe void Play()
        {
            InputStructure* iptStructure = RecordingTool.World.InputStructure;

            var actions = iptStructure->EntryActions;
            for (int i = 0; i < actions.Length; i++) {
                actions[i]->validCount = ValidCounts[i];
            }

            RecordingTool.World.GlobalRandomizer = globalRandomizer;
        }
    }
}
