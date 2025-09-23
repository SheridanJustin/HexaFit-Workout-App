using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using FitnessApp.Models;

namespace FitnessApp
{
    public class SelectExercisePageViewModel : INotifyPropertyChanged
    {
        private string searchQuery;
        private ObservableCollection<Exercise> _exercises;
        private ObservableCollection<Exercise> _defaultExercises;

        public ObservableCollection<Exercise> FilteredExercises { get; set; }

        public string SearchQuery
        {
            get => searchQuery;
            set
            {
                if (searchQuery != value)
                {
                    searchQuery = value;
                    OnPropertyChanged(nameof(SearchQuery));
                    FilterExercises();
                }
            }
        }

        public ObservableCollection<Exercise> Exercises
        {
            get => _exercises;
            set
            {
                if (_exercises != value)
                {
                    _exercises = value;
                    OnPropertyChanged(nameof(Exercises));
                }
            }
        }

        public SelectExercisePageViewModel()
        {
            _defaultExercises = new ObservableCollection<Exercise>
            {
                new Exercise { Name = "Dumbbell Bench Press", ImagePath = "dumbbell_bench_press_2.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Incline Dumbbell Bench Press", ImagePath = "dumbbell_incline_bench_press_2.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Arnold Press", ImagePath = "arnold_press_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Back Extension on Stability Ball", ImagePath = "back_extension_on_stability_ball_1.svg", MuscleGroup = "Back" },
 new Exercise { Name = "Barbell Front Raises", ImagePath = "barbell_front_raises_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Barbell Shrugs", ImagePath = "barbell_shrugs_1.svg", MuscleGroup = "Traps" },
 new Exercise { Name = "Barbell Upright Rows", ImagePath = "barbell_upright_rows_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Bench Dips", ImagePath = "bench_dips_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Bench Press", ImagePath = "bench_press_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Bent Arm Pullover", ImagePath = "bent_arm_pullover_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Bent Knee Hip Raise", ImagePath = "bent_knee_hip_raise_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Biceps Curl", ImagePath = "biceps_curl_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Biceps Curl Reverse", ImagePath = "biceps_curl_reverse_1.svg", MuscleGroup = "Forearms" },
 new Exercise { Name = "Bicep Curls", ImagePath = "bicep_curls_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Bicep Hammer Curl", ImagePath = "bicep_hammer_curl_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Bridge", ImagePath = "bridge_1.svg", MuscleGroup = "Glutes" },
 new Exercise { Name = "Concentration Curls", ImagePath = "concentration_curls_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Cross Body Crunch", ImagePath = "cross_body_crunch_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Crunches", ImagePath = "crunches_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Crunches with Legs on Stability Ball", ImagePath = "crunches_with_legs_on_stability_ball_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Decline Crunch", ImagePath = "decline_crunch_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Dumbbell Decline Flys", ImagePath = "dumbbell_decline_flys_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Dumbbell Flys", ImagePath = "dumbbell_flys_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Dumbbell Front Raises", ImagePath = "dumbbell_front_raises_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Dumbbell Lateral Raises", ImagePath = "dumbbell_lateral_raises_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Exercise Ball Pull-In", ImagePath = "exercise_ball_pull_in_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Gironda Sternum Chins", ImagePath = "gironda_sternum_chins_1.svg", MuscleGroup = "Back" },
 new Exercise { Name = "Hammer Curls with Rope", ImagePath = "hammer_curls_with_rope_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "High Cable Curls", ImagePath = "high_cable_curls_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Hyperextensions", ImagePath = "hyperextensions_1.svg", MuscleGroup = "Back" },
 new Exercise { Name = "Incline Triceps Extensions", ImagePath = "incline_triceps_extensions_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Kneeling Concentration Triceps Extension", ImagePath = "kneeling_concentration_triceps_extension_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Leg Press", ImagePath = "leg_press_1_1024x670.svg", MuscleGroup = "Quads" },
 new Exercise { Name = "Leg Raises", ImagePath = "leg_raises_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Low Triceps Extension", ImagePath = "low_triceps_extension_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Lunges", ImagePath = "lunges_1.svg", MuscleGroup = "Quads" },
 new Exercise { Name = "Lying Bicep Cable Curl", ImagePath = "lying_bicep_cable_curl_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Lying Close Grip Triceps Press to Chin", ImagePath = "lying_close_grip_triceps_press_to_chin_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Lying One Arm Rear Lateral Raise", ImagePath = "lying_one_arm_rear_lateral_raise_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Lying Rear Lateral Raise", ImagePath = "lying_rear_lateral_raise_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Lying Triceps Extension Across Face", ImagePath = "lying_triceps_extension_across_face_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Medicine Ball Biceps Curl on Stability Ball", ImagePath = "medicine_ball_biceps_curl_on_stability_ball_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Narrow Grip Bench Press", ImagePath = "narrow_grip_bench_press_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "One Armed Biased Push Up", ImagePath = "one_armed_biased_push_up_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "One Arm Bench Press", ImagePath = "one_arm_bench_press_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "One Arm Bicep Concentration on Stability Ball", ImagePath = "one_arm_bicep_concentration_on_stability_ball_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "One Arm Preacher Curl", ImagePath = "one_arm_preacher_curl_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "One Arm Shoulder Press", ImagePath = "one_arm_shoulder_press_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "One Arm Triceps Extension", ImagePath = "one_arm_triceps_extension_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "One Arm Upright Row", ImagePath = "one_arm_upright_row_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Preacher Curl", ImagePath = "preacher_curl_3_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Pullover on Stability Ball with Weight", ImagePath = "pullover_on_stability_ball_with_weight_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Push Ups", ImagePath = "push_ups_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Push Up with Feet on an Exercise Ball", ImagePath = "push_up_with_feet_on_an_exercise_ball_1.svg", MuscleGroup = "Chest" },
 new Exercise { Name = "Rear Deltoid Row", ImagePath = "rear_deltoid_row_1.svg", MuscleGroup = "Shoulders" },
 new Exercise { Name = "Reverse Plate Curls", ImagePath = "reverse_plate_curls_1.svg", MuscleGroup = "Forearms" },
 new Exercise { Name = "Seated Triceps Press", ImagePath = "seated_triceps_press_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Side Plank", ImagePath = "side_plank_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Spider Curl", ImagePath = "spider_curl_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Squats", ImagePath = "squats_1.svg", MuscleGroup = "Quads" },
 new Exercise { Name = "Stability Ball Abdominal Crunch", ImagePath = "stability_ball_abdominal_crunch_1.svg", MuscleGroup = "Abs" },
 new Exercise { Name = "Standing Biceps Curl", ImagePath = "standing_biceps_curl_1.svg", MuscleGroup = "Biceps" },
 new Exercise { Name = "Supermans", ImagePath = "supermans_1.svg", MuscleGroup = "Back" },
 new Exercise { Name = "Triceps Kickback", ImagePath = "triceps_kickback_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "Tricep Dips", ImagePath = "tricep_dips_1.svg", MuscleGroup = "Triceps" },
 new Exercise { Name = "T Bar Row", ImagePath = "t_bar_row_1.svg", MuscleGroup = "Back" }
            };

            LoadExercises();
            FilteredExercises = new ObservableCollection<Exercise>(Exercises);
        }

        private void LoadExercises()
        {
            Exercises = new ObservableCollection<Exercise>(_defaultExercises);

            if (Preferences.ContainsKey("CustomExercises"))
            {
                string serializedExercises = Preferences.Get("CustomExercises", string.Empty);
                var customExercises = JsonConvert.DeserializeObject<ObservableCollection<Exercise>>(serializedExercises);
                foreach (var exercise in customExercises)
                {
                    Exercises.Add(exercise);
                }
            }
        }

        private void FilterExercises()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredExercises = new ObservableCollection<Exercise>(Exercises);
            }
            else
            {
                var filtered = Exercises.Where(ex => ex.Name.ToLower().Contains(SearchQuery.ToLower())).ToList();
                FilteredExercises = new ObservableCollection<Exercise>(filtered);
            }

            OnPropertyChanged(nameof(FilteredExercises));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddExercise(Exercise newExercise)
        {
            Exercises.Add(newExercise);
            SaveCustomExercises();
            FilterExercises();
        }

        private void SaveCustomExercises()
        {
            var customExercises = Exercises.Except(_defaultExercises).ToList();
            string serializedExercises = JsonConvert.SerializeObject(customExercises);
            Preferences.Set("CustomExercises", serializedExercises);
        }
    }
}