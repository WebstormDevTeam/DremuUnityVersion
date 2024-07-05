using System.Collections.Generic;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using Simple.Gameplay.Tool;
namespace Simple.ChartEdit.Envelope
{
    public static class CurveTypeContextHelper
    {
        public static List<ContextMenuItem> CurveTypeGen(System.Type type, ControlNode targetNode, UnityAction dirtyCall)
        {
            List<ContextMenuItem> list = new List<ContextMenuItem>();
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                object[] attr = field.GetCustomAttributes(typeof(LabelTextAttribute), true);
                if (attr != null && attr.Length > 0)
                {
                    LabelTextAttribute descAttr = attr[0] as LabelTextAttribute;

                    list.Add(new ContextMenuItem
                    (
                        descAttr.Text, () => { targetNode.ControlType = field.GetValue(null) as CurveType? ?? CurveType.Linear ; dirtyCall(); } 
                    ));
                }
            }
            return list;
        }
    }
}