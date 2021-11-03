using Ray2Mod.Game;
using System;
using System.Collections.Generic;

namespace Ray2Mod_RecordingTool {

    public class Recording {

        public long Frame;
        public long Length;
        public Dictionary<long, FrameData> RecordingData = new Dictionary<long, FrameData>();

        public void RecordFrame()
        {
            RecordingData[Frame] = new FrameData(Frame).Record();
            if (++Frame > Length) {
                Length = Frame;
            }
        }

        /// <summary>
        /// Plays a frame of the recording.
        /// </summary>
        /// <returns>True if there's more frames, false if the playback has finished</returns>
        public bool PlayFrame()
        {
            if (RecordingData == null) {
                throw new NullReferenceException("Frame hasn't been recorded yet!");
            }
            RecordingData[Frame].Play();
            return (++Frame < Length);
        }

        public void Rewind()
        {
            Frame = 0;
        }
    }
}
