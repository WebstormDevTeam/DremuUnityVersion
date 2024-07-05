using System.Collections;
using UnityEngine;

namespace Simple.ChartEdit.Envelope
{
    public class SnipHelper
    {
        public static float Snip(float value)
        {
            return Mathf.Round(value * 4) / 4;
        }
    }
}