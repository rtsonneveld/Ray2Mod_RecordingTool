using Ray2Mod;
using Ray2Mod.Components;
using Ray2Mod.Components.Text;
using Ray2Mod.Game;
using Ray2Mod.Game.Functions;
using Ray2Mod.Game.Structs.Dynamics.Blocks;
using Ray2Mod.Game.Structs.SPO;
using Ray2Mod.Structs.Input;
using Ray2Mod.Utils;

namespace Ray2Mod_RecordingTool {

    public unsafe class RecordingTool : IMod {

        public static RemoteInterface ri;
        public static HookManager hm;
        public static World World;

        public const int counterMultiplier = (1000 / 60) * 10000;
        public static int frameCounter = 0;

        public Recording CurrentRecording { get; set; }

        private int pressEscapeTimer = 0;
        private int pressEscapeDelay = 10;
        private bool jumpShortTap = false;

        public enum RecordingState {
            Waiting,
            ArmedForRecording,
            Recording,
            ArmedForPlayback,
            Playback,
            Jump
        }

        public RecordingState State;

        private int jumpTimer = 0;

        private DynamicsBlockBase jumpStartDynamics;

        private void StartRecording()
        {
            CurrentRecording = new Recording();
            State = RecordingState.Recording;
        }

        private void StopRecording()
        {
            State = RecordingState.Waiting;
        }

        private void StartPlayback()
        {
            if (CurrentRecording != null) {
                CurrentRecording.Rewind();
                State = RecordingState.Playback;
            }
        }

        private void StopPlayback()
        {
            State = RecordingState.Waiting;
        }

        private string StateString(RecordingState state, Recording recording)
        {
            long frame = recording!=null ? recording.Frame : 0;
            switch (state) {
                case RecordingState.ArmedForRecording: return "Armed for Recording";
                case RecordingState.ArmedForPlayback: return "Armed for Playback";
                case RecordingState.Recording: return $"Recorded {frame} frames";
                case RecordingState.Playback: return $"Playback, frame {frame}";
                default: return state.ToString();
            }
        }

        private short ReadInputHook(int a1)
        {
            short result = 0;

            if (State == RecordingState.Jump) {

                jumpTimer++;

                if (jumpTimer<250) {

                    if (jumpTimer == 30) {
                        World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount = 1;
                    }

                    if (jumpTimer > 30 && jumpTimer < 30 + pressEscapeDelay) {
                        World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount = 2;
                    }

                    if (jumpTimer == 30 + pressEscapeDelay) {
                        World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount = -1;
                    }

                    if (jumpTimer == 30 + pressEscapeDelay + 1) {
                        World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount = 1;
                    }

                    if (jumpTimer > 30 + pressEscapeDelay + 1) {
                        World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount = jumpShortTap?-2:2;
                    }

                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Clavier_Haut]->validCount = 2;

                    ri.Log("jumpTimer " + jumpTimer + ", SAUTER = " + World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount);

                }  else if (jumpTimer == 250) {
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Clavier_Haut]->validCount = -2;
                    if (pressEscapeDelay++ > 80) {
                        State = RecordingState.Waiting;
                        pressEscapeDelay = 10;
                    }

                } else if (jumpTimer == 280) {
                    (*(SuperObject**)Offsets.MainChar)->PersoData->dynam->DynamicsBase->DynamicsBlockBase = jumpStartDynamics;
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Camera_Cut]->validCount = 1;
                } else if (jumpTimer == 360) {
                    jumpTimer = 0;
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Camera_Cut]->validCount = -2;
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Clavier_Haut]->validCount = -2;
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount = -2;
                }

                return 0;

            } else {

                if (CurrentRecording != null) {

                    if (State == RecordingState.Recording) {
                        result = InputFunctions.VReadInput.Call(a1);
                        CurrentRecording.RecordFrame();
                        return result;
                    }

                    if (State == RecordingState.Playback) {
                        if (!CurrentRecording.PlayFrame()) {
                            StopPlayback();
                        }

                        return 0;
                    }

                }

            }

            result = InputFunctions.VReadInput.Call(a1);

            return result;
        }

        private int QueryPerformanceCounterHook(short a1, long* lpPerformanceCount)
        {
            *lpPerformanceCount = frameCounter * counterMultiplier;
            return 1;
        }

        unsafe void IMod.Run(RemoteInterface remoteInterface)
        {
            ri = remoteInterface;
            World = new World();

            GlobalInput.Actions['r'] = () =>
            {
                switch (State) {
                    case RecordingState.Waiting: State = RecordingState.ArmedForRecording; break;
                    case RecordingState.ArmedForRecording: State = RecordingState.Waiting; break;
                    case RecordingState.Recording: StopRecording(); break;
                }
            };

            GlobalInput.Actions['p'] = () =>
            {
                switch (State) {
                    case RecordingState.Waiting: State = RecordingState.ArmedForPlayback; break;
                    case RecordingState.ArmedForPlayback: State = RecordingState.Waiting; break;
                    case RecordingState.Playback: StopPlayback(); break;
                }
            };

            GlobalInput.Actions['g'] = () =>
            {
                State = RecordingState.Jump;
                jumpTimer = 0;
                jumpStartDynamics = (*(SuperObject**)Offsets.MainChar)->PersoData->dynam->DynamicsBase->DynamicsBlockBase;
            };

            GlobalInput.Actions['u'] = () =>
            {
                pressEscapeDelay--;
            };
            GlobalInput.Actions['i'] = () =>
            {
                pressEscapeDelay++;
            };
            GlobalInput.Actions['y'] = () =>
            {
                jumpShortTap = !jumpShortTap;
            };

            TextOverlay status = new TextOverlay((previousText) =>
            {
                return $"Recording Tool" + TextUtils.Arrow + StateString(State, CurrentRecording);
            }, 10, 5, 5).Show();

            TextOverlay delayText = new TextOverlay((previousText) =>
            {
                return $"Delay (U,I)" + TextUtils.Arrow + pressEscapeDelay.ToString();
            }, 10, 5, 30).Show();

            TextOverlay hoverTapText = new TextOverlay((previousText) =>
            {
                return $"Short Tap Hover (Y)" + TextUtils.Arrow + jumpShortTap.ToString();
            }, 10, 5, 60).Show();

            GlobalActions.PreEngine += () =>
            {
                frameCounter++;
                /*
                if (pressEscapeTimer-- == 0) {
                    ri.Log("ESCAPE ESCAPE OPT DELETE");
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Menu_Entrer]->validCount = 1;
                }
                if (pressEscapeTimer < 0 && pressEscapeTimer > -10) {
                    ri.Log("ESCAPE ESCAPE OPT DELETE ("+pressEscapeTimer+")");
                    World.InputStructure->EntryActions[(int)EntryActionNames.Action_Menu_Entrer]->validCount = 2-pressEscapeTimer;
                }*/
            };

            hm = new HookManager();
            hm.CreateHook(InputFunctions.VReadInput, ReadInputHook);
            hm.CreateHook(EngineFunctions.DoQueryPerformanceCounter, QueryPerformanceCounterHook);

            GlobalActions.EngineStateChanged += (previous, current) =>
            {
                if (previous!=current) {
                    if (current == EnumEngineState.STATE_9_LOADED) {

                        if (State == RecordingState.Jump) {
                            State = RecordingState.Waiting;
                            jumpTimer = 0;
                        }

                        pressEscapeTimer = pressEscapeDelay;

                        if (State == RecordingState.ArmedForRecording) {
                            StartRecording();
                        } else if (State == RecordingState.ArmedForPlayback) {
                            StartPlayback();
                        }
                    } else if (State == RecordingState.Recording) {
                        StopRecording();
                    }
                }
            };
        }
    }
}
