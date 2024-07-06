///////////////////////DebugCode///////////////////////
#if UNITY_EDITOR
#define AUTOPLAY
#endif

#if DEBUG
#define  AUTOPLAY
#endif

#define AUTOPLAY
///////////////////////DebugCode///////////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Gameplay.Tool;
using System;
using Dremu.Gameplay.Object;
using Dremu.Gameplay.Tool;

namespace Dremu.Gameplay.Manager {
    public class NoteManager : MonoBehaviour
    {


        [SerializeField] Tap _Tap;
        [SerializeField] Slide _Slide;
        [SerializeField] Hold _Hold;
        [SerializeField] Drag _Drag;

        StandardObjectPool<Tap> TapPool;
        StandardObjectPool<Slide> SlidePool;
        StandardObjectPool<Hold> HoldPool;
        StandardObjectPool<Drag> DragPool;

        readonly List<Tap> ActiveTaps = new List<Tap>();
        readonly List<Slide> ActiveSlides = new List<Slide>();
        readonly List<Hold> ActiveHolds = new List<Hold>();
        readonly List<Drag> ActiveDrags = new List<Drag>();

        public static Color NoteColor { get; private set; }

        public static NoteManager Instance { get; private set; }

        private void Awake() {
            Instance = this;

            TapPool = new StandardObjectPool<Tap>(_Tap, 5);
            SlidePool = new StandardObjectPool<Slide>(_Slide, 10);
            HoldPool = new StandardObjectPool<Hold>(_Hold, 1);
            DragPool = new StandardObjectPool<Drag>(_Drag, 2);

            NoteColor = Color.black;

        }

        /// <summary>
        /// 创建一个新的Tap音符
        /// </summary>
        /// <param name="JudgmentLine">判定线</param>
        /// <param name="Position">相对位置</param>
        /// <param name="ArrivalTime">到达时间</param>
        /// <returns></returns>
        public static Tap GetNewTap( JudgmentLine JudgmentLine, float Position, float ArrivalTime ) {
            Tap tap = Instance.TapPool.GetObject();
            tap.SetArrivalTime(ArrivalTime);
            tap.SetPosition(Position);
            tap.SetWidth(JudgmentLine.NoteWidth);
            JudgmentLine.AddNote(tap);
            Instance.ActiveTaps.Add(tap);
            return tap;
        }


        /// <summary>
        /// 创建一个新的Slide音符
        /// </summary>
        /// <param name="JudgmentLine">判定线</param>
        /// <param name="Position">相对位置</param>
        /// <param name="ArrivalTime">到达时间</param>
        /// <returns></returns>
        public static Slide GetNewSlide( JudgmentLine JudgmentLine, float Position, float ArrivalTime ) {
            Slide slide = Instance.SlidePool.GetObject();
            slide.SetArrivalTime(ArrivalTime);
            slide.SetPosition(Position);
            slide.SetWidth(JudgmentLine.NoteWidth);
            JudgmentLine.AddNote(slide);
            Instance.ActiveSlides.Add(slide);
            return slide;
        }

        /// <summary>
        /// 创建一个新的Hold音符
        /// </summary>
        /// <param name="JudgmentLine">判定线</param>
        /// <param name="Position">相对位置</param>
        /// <param name="ArrivalTime">到达时间</param>
        /// <param name="HoldNodes">判定节点</param>
        /// <returns></returns>
        public static Hold GetNewHold( JudgmentLine JudgmentLine, float Position, float ArrivalTime, List<Hold.HoldNode> HoldNodes ) {
            Hold hold = Instance.HoldPool.GetObject();
            hold.SetArrivalTime(ArrivalTime);
            hold.SetPosition(Position);
            hold.SetHoldNodes(HoldNodes);
            JudgmentLine.AddNote(hold);
            Instance.ActiveHolds.Add(hold);
            return hold;
        }

        /// <summary>
        /// 创建一个新的Drag音符
        /// </summary>
        /// <param name="JudgmentLine">判定线</param>
        /// <param name="Position">相对位置</param>
        /// <param name="ArrivalTime">到达时间</param>
        /// <param name="DragNodes">判定节点</param>
        /// <returns></returns>
        public static Drag GetNewDrag( JudgmentLine JudgmentLine, float Position, float ArrivalTime, List<Drag.DragNode> DragNodes ) {
            Drag drag = Instance.DragPool.GetObject();
            drag.SetArrivalTime(ArrivalTime);
            drag.SetPosition(Position);
            drag.SetDragNodes(DragNodes);
            JudgmentLine.AddNote(drag);
            Instance.ActiveDrags.Add(drag);
            return drag;
        }

        /// <summary>
        /// 更新音符的状态
        /// 此方法只能由主控调用
        /// </summary>
        /// <param name="CurrentTime">当前时间</param>
        public static void UpdateNoteState( float CurrentTime ) {
            List<NoteBase> HitableNote = new List<NoteBase>();
            float CurrentSecond = MainController.BPM.GetSecondFromBeat(CurrentTime);
            // Debug.Log(CurrentSecond);


            //处理各种note的活动事件并且选出处在可判定范围内的note
            foreach (var tap in Instance.ActiveTaps) {
                tap.OnActive(CurrentTime);
                if (MainController.BPM.GetSecondFromBeat(tap.ArrivalTime) - CurrentSecond <= 0.15)
                    HitableNote.Add(tap);
            }
            foreach (var slide in Instance.ActiveSlides) {
                slide.OnActive(CurrentTime);
                if (MainController.BPM.GetSecondFromBeat(slide.ArrivalTime) - CurrentSecond <= 0.05)
                    HitableNote.Add(slide);
            }
            foreach (var hold in Instance.ActiveHolds) {
                hold.OnActive(CurrentTime);
                if (MainController.BPM.GetSecondFromBeat(hold.ArrivalTime) - CurrentSecond <= 0.08)
                    HitableNote.Add(hold);
            }
            foreach (var drag in Instance.ActiveDrags) {
                drag.OnActive(CurrentTime);
                if (MainController.BPM.GetSecondFromBeat(drag.ArrivalTime) - CurrentSecond <= 0.05)
                    HitableNote.Add(drag);
            }


            //处理可判定范围内的note
#if AUTOPLAY
            //奥托主教（不是）autoplay
            foreach (var note in HitableNote) {
                if (note is Tap tap) {
                    if (tap.ArrivalTime <= CurrentTime) {
                        ScoreManager.AddPerfect();
                        NoteEffectManager.GetNewNoteEffect(tap.transform.position);
                        Instance.ActiveTaps.Remove(tap);
                        Instance.TapPool.ReturnObject(tap);
                    }
                }
                else if (note is Slide slide) {
                    if (slide.ArrivalTime <= CurrentTime) {
                        ScoreManager.AddPerfect();
                        NoteEffectManager.GetNewNoteEffect(slide.transform.position);
                        Instance.ActiveSlides.Remove(slide);
                        Instance.SlidePool.ReturnObject(slide);
                    }
                }
                else if (note is Hold hold) {
                    if (hold.ArrivalTime <= CurrentTime) {
                        ScoreManager.AddPerfect();
                        if (CurrentSecond - hold.NoteEffectTimer >= 0.2) {
                            NoteEffectManager.GetNewNoteEffect(hold.transform.position);
                            hold.NoteEffectTimer = CurrentSecond;
                        }
                        if (hold.IsEnd(CurrentTime)) {
                            Instance.ActiveHolds.Remove(hold);
                            Instance.HoldPool.ReturnObject(hold);
                        }
                    }
                }
                else if (note is Drag drag) {
                    if (drag.ArrivalTime <= CurrentTime) {
                        ScoreManager.AddPerfect();
                        if (CurrentSecond - drag.NoteEffectTimer >= 0.2) {
                            NoteEffectManager.GetNewNoteEffect(drag.transform.position);
                            drag.NoteEffectTimer = CurrentSecond;
                        }
                        if (drag.IsEnd(CurrentTime)) {
                            Instance.ActiveDrags.Remove(drag);
                            Instance.DragPool.ReturnObject(drag);
                        }
                    }
                }
            }
#else
            
#endif
        }

    }
}