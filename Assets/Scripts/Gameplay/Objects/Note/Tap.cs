using Simple.Gameplay.Manager;
using Simple.Gameplay.Object;
using Simple.Gameplay.Tool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils.Helper;

namespace Simple.Gameplay.Tool {
    public static class Functions {
        public static List<Vector3> Vec2ListToVec3List( List<Vector2> vector2s ) {
            List<Vector3> list = new List<Vector3>(vector2s.Count);
            foreach (Vector2 v in vector2s) {
                list.Add(v);
            }
            return list;
        }
    }

}

namespace Simple.Gameplay.Object {

    public sealed class Tap : NoteBase {
        [SerializeField] LineRenderer Line;
        [SerializeField] SpriteRenderer Renderer;


        private float Width;

        public override void OnActive( float CurrentTime ) {
            //实时更新形状
            var points = new List<Vector2>(JudgmentLine.CurrentCurve.SubCurveByMidAndLength(position, Width));
            for (int i = 0; i < points.Count; i++)
                points[i] = PositionHelper.RelativeCoordToAbsoluteCoord(points[i], Camera.main);

            Line.positionCount = points.Count;
            Line.SetPositions(Functions.Vec2ListToVec3List(points).ToArray());

            //设置音符位置
            KeyValuePair<Vector2, Vector2> normal = JudgmentLine.CurrentCurve.GetNormal(position);
            transform.localPosition = PositionHelper.RelativeCoordToAbsoluteCoord(normal.Key, Camera.main) + normal.Value * JudgmentLine.Speed.GetPosition(CurrentTime, ArrivalTime - CurrentTime);

            if (CurrentTime > ArrivalTime) //淡出
                Renderer.color = Line.startColor = Line.endColor = UGUIHelper.SetAlpha(NoteManager.NoteColor, 1 - (CurrentTime - ArrivalTime)*2);
        }

        public override void OnInitialize() {

        }

        public override void OnRecycle() {

        }


        /// <summary>
        /// 设置音符宽度
        /// </summary>
        /// <param name="Width">宽度</param>
        public void SetWidth( float Width ) {
            this.Width = Width;
        }
    }

}
