using System.Collections;
using System.Collections.Generic;
// using System.Diagnostics;
using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Dremu.Gameplay.Tool
{
    public sealed class EnvelopeLine
    {
        public List<ControlNode> Controllers;

        // 兼容
        [Obsolete]
        public EnvelopeLine(List<float[]> Points)
        {
            Controllers = new List<ControlNode>();
            foreach(var point in Points)
            {
                // 0: time 1: value
                Controllers.Add(new ControlNode(point[0], point[1], 0,CurveType.Linear));
            }
        }

        public EnvelopeLine(List<ControlNode> Controllers)
        {
            this.Controllers = Controllers;
        }

        /// <summary>
        /// 返回包络线在Time处的值.
        /// </summary>
        /// <param name="Time"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">正常的数据不会抛出, 当Controllers处于非法状态 (如在某个时间处没有定义) 时才会</exception>
        public float GetValue(float Time)
        {
            if (Controllers.Count == 0)
                // 继承先辈 (乙酸) 意愿
                return -114514;
            int currentControlerPos = GetControlerIndex(this.Controllers, Time);
            if (currentControlerPos == -1)
                throw new System.Exception("找不到这个时间啊, 不是从0开始的罢");
            ControlNode controler = Controllers[currentControlerPos];
            if (currentControlerPos == this.Controllers.Count - 1)
            {
                // 谱面结束力! 卡住不动就行了
                
                return controler.Value;
            }
            ControlNode nextControler = Controllers[currentControlerPos + 1];
            var value = GetSingle(controler,nextControler,Time);
            return value;
        }

        /// <summary>
        /// 由某个Node和下一个Node得到此Node在Time时的值
        /// </summary>
        /// <param name="Start"></param>
        /// <param name="End"></param>
        /// <param name="Time"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public float GetSingle(ControlNode Start,ControlNode End,float Time)
        {
            // 似乎这俩相等的时候,这个Node里没包含任何点, 为兼容这种情况使用下面的if
            // 在edit的时候优先考虑后面的Node
            if (Time == Start.Time && Time == End.Time)
            {
                return End.Value;
            }
            if (Time < End.Time && Time >= Start.Time)
            {
                float percent = (Time - Start.Time) / (End.Time - Start.Time);
                return Start.ValueGetPercent(percent) * (End.Value - Start.Value) + Start.Value;
            }
            else
            {
                // ( xx -> [yy,zz) )
                throw new ArgumentOutOfRangeException("你这time保在范围内吗? " +
                    "( " + Time.ToString() + " -> " +
                        "[" + Start.Time.ToString() + "," + End.Time.ToString() + ") " +
                    ")");
            }
        }

        /// <summary>
        /// 找到在某时间处的Node
        /// 二分搜索实现
        /// </summary>
        /// <param name="controlerList"></param>
        /// <param name="Time"></param>
        /// <param name="Low"></param>
        /// <param name="High"></param>
        /// <returns>找到的位置, 未找到返回-1</returns>
        public static int GetControlerIndex(List<ControlNode> controlerList, float Time, int Low = 0, int High = -1)
        {
            if (High == -1)
            {
                High = controlerList.Count;
            }

            while (Low < High)
            {
                int middle = Low + ((High - Low) >> 1);
                if (Time < controlerList[middle].Time)
                {
                    High = middle;
                }
                else if (middle == controlerList.Count - 1 || (Time >= controlerList[middle].Time && controlerList[middle + 1].Time > Time))
                {
                    return middle;
                }
                else
                {
                    Low = middle + 1;
                }
            }
            return -1;

        }

        /// <summary>
        /// 当存储的数据为速度时, 获取位置
        /// </summary>
        /// <param name="StartTime">起始时间</param>
        /// <param name="Duration">持续时间</param>
        /// <returns></returns>
        public float GetPosition( float StartTime, float Duration ) {
            float result = 0;
            float sign = Mathf.Sign(Duration);
            Duration = Mathf.Abs(Duration);
            if (Controllers.Count == 0)
                return -114514; //没有控制点可是会受到惩罚的哦（意味深）
            for (int i = 0; i < Controllers.Count - 1; i++) {
                ControlNode currentController = Controllers[i], nextControler = Controllers[i + 1];
                if (currentController.Time < StartTime) continue;
                else if (currentController.Time < StartTime && nextControler.Time > StartTime) {
                    float time = nextControler.Time - StartTime;
                    result += ControlNode.GetArea(currentController, nextControler, 1) -
                              ControlNode.GetArea(currentController, nextControler, 1 - time / (nextControler.Time - currentController.Time));
                    Duration -= time;
                }
                else {
                    float time = Mathf.Min(Duration, nextControler.Time - currentController.Time);
                    result += ControlNode.GetArea(currentController, nextControler, time / (nextControler.Time - currentController.Time));
                    Duration -= time;
                }
            }
            if (Duration > 0) {
                result += Duration * Controllers[^1].Value;
            }
            return sign * result;
        }

        /// <summary>
        /// 当存储数据为bpm时, 获取秒数时间
        /// 此时线中数据的time都以秒为单位
        /// </summary>
        /// <param name="Beat">节拍数</param>
        public float GetBeatFromSecond(float second)
        {
            float fullBeatUpToNow = 0;
            for (int i = 0; i < this.Controllers.Count - 1; i++)
            {
                var ctrl = Controllers[i];
                var nextCtrl = Controllers[i + 1];
                var timeDelta = nextCtrl.Time - ctrl.Time;
                if (ctrl.Time <= second && nextCtrl.Time > second)
                {
                    return fullBeatUpToNow + ControlNode.GetArea(ctrl, nextCtrl, (second - ctrl.Time) / timeDelta)/60f;
                }
                else
                {
                    fullBeatUpToNow += ControlNode.GetArea(ctrl, nextCtrl, 1)/60f;
                }
            }
            // 如果一直没找到
            return Controllers[^1].Value / 60f * (second - Controllers[^1].Time) + fullBeatUpToNow;
        }

        public float GetSecondFromBeat(float beat)
        {
            float fullBeatUpToNow = 0;
            for (int i = 0; i < this.Controllers.Count - 1; i++)
            {
                var ctrl = Controllers[i];
                var nextCtrl = Controllers[i + 1];
                // 以后可以考虑把fullBeatUpToNow 记录在每个事件中, 这部分运行时不需要改变
                var timeDelta = nextCtrl.Time - ctrl.Time;
                var changedBeat = ControlNode.GetArea(ctrl, nextCtrl, 1)/60f;
                if (fullBeatUpToNow + changedBeat > beat)
                {
                    var percent =  ControlNode.GetPercent(ctrl, nextCtrl, (beat - fullBeatUpToNow) * 60f);
                    Debug.Assert(percent <= 1 && percent >= 0, "Error in find area, 请让液氦check the code");
                    return percent * timeDelta + ctrl.Time;
                }
                else
                {
                    fullBeatUpToNow += changedBeat;
                }
            }

            return Controllers[^1].Time + (beat-fullBeatUpToNow) / (Controllers[^1].Value / 60f);
        }


        
    }

    /// <summary>
    /// 希望谁能给个更好的名称
    /// </summary>
    public class ControlNode
    {
        public float Time { get; set; }
        public float Value { get; set; }
        [Range(0, 1)] public float Tension;

        public CurveType ControlType;

        static float EaseInExpo(float x) => 1 - Mathf.Pow(2f, -10 * x);
        static float Sine(float x) => Mathf.Sin(Mathf.PI * x / 2);

        public ControlNode(float Time, float Value, float Tension, CurveType CurveType)
        {
            this.Time = Time;
            this.Value = Value;
            this.Tension = Mathf.Clamp01(Tension);
            this.ControlType = CurveType;
        }

        
        public float ValueGetPercent(float percent)
        {
            return ControlType switch
            {
                CurveType.Linear => percent,
                CurveType.Const => 0,
                CurveType.Expo => GetValueFromEaseFunc(EaseInExpo,percent),
                CurveType.Sine => GetValueFromEaseFunc(Sine, percent),
                _ => 0
            };
        }

        private float GetValueFromEaseFunc(Func<float, float> p, float x)
        {
            var k = 2 * Tension - 1;
            Debug.Assert(k <= 1 && k >= -1);
            float f;
            if (k >= 0)
            {
                f = p(x);
            } else 
            {
                f = -p(1-x) + 1;
            }
            return Mathf.Abs(f - x) * k + x;
        }

        /// <summary>
        /// 获取两个控制点之间缓动曲线与x轴围成的图形的面积（近似）
        /// </summary>
        /// <param name="Start">起始控制点</param>
        /// <param name="End">结束控制点</param>
        /// <param name="Percent">完整度</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">开始时间大于结束时间时抛出</exception>
        public static float GetArea(ControlNode Start, ControlNode End, float Percent ) {
            if (Start.Time > End.Time)
                throw new ArgumentException("Start is larger than End！Start:" + Start.Time + " End:" + End.Time); //开始时间比结束时间大了十倍甚至九倍（悲）
            if (Start.Time == End.Time)
                return 0; //时间一样直接返回0
            if (Start.Value == End.Value || Start.ControlType == CurveType.Const) //水平或常量，矩形面积
                return (End.Time - Start.Time /*长*/ ) * Start.Value /*宽*/ * Percent;
            if (Start.ControlType == CurveType.Linear) //线性，梯形面积
                return (End.Time - Start.Time ) * Percent /*高*/ * (2 * Start.Value + (End.Value - Start.Value /*上底加下底*/ ) * Percent) / 2f;
            float area = 0;
            float currentValue = Start.Value;
            int devideCount = Mathf.Max(1, (int)((End.Time - Start.Time) * Percent / 0.5f)); //微分片段数，越多结果越精确
            for (int i = 0; i < devideCount; i++) {
                float nextValue = Start.Value + (End.Value - Start.Value) * Start.ValueGetPercent(Percent * i / devideCount); //获取下一个数值
                area += (currentValue + nextValue /*上底加下底*/ ) * (End.Time - Start.Time) * Percent * i / devideCount /*高*/ / 2; //计算梯形面积
                currentValue = nextValue; //将当前数值设置成下一个数值
            }
            return area;
        }

        /// <summary>
        /// 二分搜索找到何时经过了指定的面积
        /// 事实上, 最坏条件下循环体大约会执行6次
        /// 随precision后面除的值增加而大致以对数增长
        /// </summary>
        /// <param name="Start"></param>
        /// <param name="End"></param>
        /// <param name="TargetValue">期望的面积</param>
        /// <param name="Low"></param>
        /// <param name="High"></param>
        /// <returns>从0-1间的一个值, 为GetArea函数的percent参数</returns>
        public static float GetPercent(ControlNode Start, ControlNode End, float TargetValue, float Low = 0, float High = 1)
        {
            float precision = GetArea(Start,End,1) / 1000;
            precision = Math.Max(0.001f, precision);
            float x;
            float midValue;
            do
            {
                x = (Low + High) / 2;
                midValue = GetArea(Start, End, x);
                var lowValue = GetArea(Start, End, Low);
                if ((midValue - TargetValue) * (lowValue - TargetValue) > 0)
                    Low = x;
                else
                    High = x;

            } while (Math.Abs(midValue - TargetValue) > precision);
            return x;
        }

    }
    public enum CurveType
    {
        [LabelText("常量")] Const,
        [LabelText("线性")] Linear,
        [LabelText("指数")] Expo,
        [LabelText("正弦")] Sine,
    }
}