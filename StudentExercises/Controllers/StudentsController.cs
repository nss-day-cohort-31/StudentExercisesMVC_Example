using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StudentExercises.Models;
using StudentExercises.Models.ViewModels;

namespace StudentExercises.Controllers
{
    public class StudentsController : Controller
    {

        private readonly IConfiguration _config;

        public StudentsController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        // GET: Students
        public ActionResult Index()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
            SELECT s.Id,
                s.FirstName,
                s.LastName,
                s.SlackHandle,
                s.CohortId
            FROM Student s
        ";
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<Student> students = new List<Student>();
                    while (reader.Read())
                    {
                        Student student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId"))
                        };

                        students.Add(student);
                    }

                    reader.Close();

                    return View(students);
                }
            }
        }

        // GET: Students/Details/5
        public ActionResult Details(int id)
        {
            Student student = GetStudentById(id);
            return View(student);
        }

        // GET: Students/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Students/Edit/5
        public ActionResult Edit(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    //we use a join to get all of the Ids of the exercises that student has assigned to them
                    cmd.CommandText = @"
                            SELECT s.Id,
                                s.FirstName,
                                s.LastName,
                                s.SlackHandle,
                                s.CohortId,
                                se.ExerciseId
                            FROM Student s
                            LEFT JOIN StudentExercise se on s.Id = se.StudentId
                            WHERE s.Id=@Id
                        ";
                    cmd.Parameters.Add(new SqlParameter("@Id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    StudentEditViewModel viewmodel = null;
                    while (reader.Read())
                    {
                        //the first time this while loop runs, the view model will be null, which will grab the student we are trying to Edit
                        if (viewmodel == null)
                        {
                            var studentId = reader.GetInt32(reader.GetOrdinal("Id"));

                            viewmodel = new StudentEditViewModel();

                            viewmodel.Student = new Student
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                                CohortId = reader.GetInt32(reader.GetOrdinal("CohortId"))
                            };
                        }
                        //this will add the selected exercises to the SelectedExercises list so it will populate in the edit form
                        if (!reader.IsDBNull(reader.GetOrdinal("ExerciseId")))
                        {
                            viewmodel.SelectedExercises.Add(
                                reader.GetInt32(reader.GetOrdinal("ExerciseId"))
                            );
                        }

                    }
                    reader.Close();
                    //this would run if we didnt find a student with the ID given to us
                    if (viewmodel == null)
                    {
                        return NotFound();
                    }
                    //populates the list of all available cohorts
                    List<Cohort> cohorts = GetAllCohorts();
                    viewmodel.AvailableCohorts = cohorts;
                    //this method retrieves all of the exercises and adds them to the availableExercises List
                    List<Exercise> exercises = GetAllExercises();
                    viewmodel.AvailableExercises = exercises;
                    return View(viewmodel);
                }
            }
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, StudentEditViewModel viewModel)
        {
            Student student = viewModel.Student;
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        //we still keep our regular update statement to update the properties of the student
                        cmd.CommandText = @"UPDATE Student
                                               SET FirstName=@FirstName,
                                                   LastName=@LastName,
                                                   SlackHandle=@slack,
                                                   CohortId=@CohortId
                                             WHERE Id=@Id";
                        cmd.Parameters.Add(new SqlParameter("@FirstName", student.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@LastName", student.LastName));
                        cmd.Parameters.Add(new SqlParameter("@slack", student.SlackHandle));
                        cmd.Parameters.Add(new SqlParameter("@CohortId", student.CohortId));
                        cmd.Parameters.Add(new SqlParameter("@Id", id));

                        cmd.ExecuteNonQuery();
                        //this clears the parameters we set above
                        cmd.Parameters.Clear();


                        // We need to delete everything from that specific student's SE table, then rebuild...

                        cmd.CommandText = "DELETE FROM StudentExercise WHERE StudentId = @StudentId";
                        cmd.Parameters.Add(new SqlParameter("@StudentId", id));
                        cmd.ExecuteNonQuery();
                        //Now we rebuild the StudentExercise tables with the newly selected exercises by inserting and looping through the list of ints of selected exercises
                        cmd.CommandText = @"INSERT INTO StudentExercise (StudentId, ExerciseId)
                                                     VALUES (@StudentId, @ExerciseId)";
                        foreach (var exerciseId in viewModel.SelectedExercises)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add(new SqlParameter("@StudentId", id));
                            cmd.Parameters.Add(new SqlParameter("@ExerciseId", exerciseId));
                            cmd.ExecuteNonQuery();
                        }

                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: Students/Delete/5
        public ActionResult Delete(int id)
        {
            Student student = GetStudentById(id);
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM StudentExercise WHERE StudentId = @Id; 
                                            DELETE FROM Student WHERE Id=@Id";

                        cmd.Parameters.Add(new SqlParameter("@Id", id));
                        cmd.ExecuteNonQuery();

                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch
            {
                return View();
            }
        }

        private Student GetStudentById(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
            SELECT s.Id,
                s.FirstName,
                s.LastName,
                s.SlackHandle,
                s.CohortId
            FROM Student s
            WHERE s.Id=@Id
        ";
                    cmd.Parameters.Add(new SqlParameter("@Id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Student student = null;
                    if (reader.Read())
                    {
                        student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId"))
                        };
                    }

                    reader.Close();

                    return student;
                }
            }
        }
        private List<Cohort> GetAllCohorts()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @" SELECT c.Id, c.Name FROM Cohort c";
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<Cohort> cohorts = new List<Cohort>();
                    while (reader.Read())
                    {
                        Cohort cohort = new Cohort
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                        };

                        cohorts.Add(cohort);
                    }

                    reader.Close();

                    return cohorts;
                }
            }
        }

        private List<Exercise> GetAllExercises()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @" SELECT e.Id, e.Name FROM Exercise e";
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<Exercise> exercises = new List<Exercise>();
                    while (reader.Read())
                    {
                        Exercise exercise = new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                        };

                        exercises.Add(exercise);
                    }

                    reader.Close();

                    return exercises;
                }
            }
        }
    }
}