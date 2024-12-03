using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkoutApplication.Effects
{
    public class TintEffect : RoutingEffect
    {
        public Color TintColor { get; set; }

        public TintEffect() : base("WorkoutApplication.TintEffect")
        {
        }
    }
}