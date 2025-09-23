namespace FitnessApp.Models
{
    // Shared WorkoutSession class
    public class WorkoutSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string LastPerformed { get; set; }
        public List<Exercise> Exercises { get; set; }
        public List<CompletedExercise> CompletedExercises { get; set; } // Used in MainPage
    }

    // Shared Exercise class
    public class Exercise
    {
        public string Name { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty; // Ensure it's initialized
        public List<Set> Sets { get; set; } = new List<Set>(); // Initialize to an empty list
    }


    // Shared Set class
    public class Set
    {
        public int SetNumber { get; set; } // Added to keep track of the set number
        public double Weight { get; set; }
        public int Reps { get; set; }
        public double PreviousWeight { get; set; } // Added to store previous weight
        public int PreviousReps { get; set; } // Added to store previous reps
        public bool Completed { get; set; }
    }

    // Shared CompletedExercise class
    public class CompletedExercise
    {
        public string Name { get; set; }
        public List<CompletedSet> Sets { get; set; }
    }

    // Shared CompletedSet class
    public class CompletedSet
    {
        public double Weight { get; set; }
        public int Reps { get; set; }
        public bool Completed { get; set; }
    }
}
