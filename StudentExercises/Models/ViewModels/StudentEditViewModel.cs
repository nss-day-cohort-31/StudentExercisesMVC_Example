using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace StudentExercises.Models.ViewModels
{
    public class StudentEditViewModel
    {
   
        public Student Student { get; set; }

        //this selected exercises list will hold all of the Ids of the exercises that were selected.
        public List<int> SelectedExercises { get; set; } = new List<int>();
        //a list of all available exercises
        public List<Exercise> AvailableExercises { get; set; }

        public List<SelectListItem> AvailableExercisesSelectList
        {
            get
            {
                if (AvailableExercises == null)
                {
                    return null;
                }
                return AvailableExercises
                       .Select(e => new SelectListItem(e.Name, e.Id.ToString()))
                       .ToList();
            }

        }

        public List<Cohort> AvailableCohorts { get; set; }
        public List<SelectListItem> AvailableCohortsSelectList
        {
            get
            {
                if (AvailableCohorts == null)
                {
                    return null;
                }
                return AvailableCohorts
                    //this select converts the cohort types in the list to SelectListItems
                       .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
                       .ToList();
            }
        }
    }
}
