using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkoutApplication
{
    public class WorkoutPlan
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Exercise> Exercises { get; set; }
    }

    public class Exercise
    {
        public string Name { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public string Duration { get; set; } // Optional for exercises that have a duration instead of sets/reps
    }
}
