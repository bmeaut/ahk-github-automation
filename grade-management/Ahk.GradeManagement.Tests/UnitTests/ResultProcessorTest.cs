using Ahk.GradeManagement.ResultProcessing;
using Ahk.GradeManagement.ResultProcessing.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ahk.GradeManagement.Tests.UnitTests
{
    [TestClass]
    public class ResultProcessorTest
    {
        [TestMethod]
        public void GetTotalPoints_EmptyArray()
        {
            var actual = ResultProcessor.GetTotalPoints(new AhkTaskResult[0]);
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void GetTotalPoints_SingleElementNoName()
        {
            var actual = ResultProcessor.GetTotalPoints(new[] { new AhkTaskResult() { Points = 1 } });
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(string.Empty, actual.First().Name);
            Assert.AreEqual(1, actual.First().Point);
        }

        [TestMethod]
        public void GetTotalPoints_TwoNonameElementsSummed()
        {
            var actual = ResultProcessor.GetTotalPoints(new[] { new AhkTaskResult() { Points = 1 }, new AhkTaskResult() { Points = 2.34 } });
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(string.Empty, actual.First().Name);
            Assert.AreEqual(3.34, actual.First().Point);
        }

        [TestMethod]
        public void GetTotalPoints_TwoNamedTaskNoExerciseElementsSummed()
        {
            var actual = ResultProcessor.GetTotalPoints(new[] { new AhkTaskResult() { TaskName = "a", Points = 1 }, new AhkTaskResult() { TaskName = "b", Points = 2.34 } });
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(string.Empty, actual.First().Name);
            Assert.AreEqual(3.34, actual.First().Point);
        }

        [TestMethod]
        public void GetTotalPoints_NamedTaskSameExerciseElementsSummed()
        {
            var actual = ResultProcessor.GetTotalPoints(new[] { new AhkTaskResult() { TaskName = "a", ExerciseName = "ex1", Points = 1 }, new AhkTaskResult() { TaskName = "b", ExerciseName = "ex1", Points = 2.34 } });
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("ex1", actual.First().Name);
            Assert.AreEqual(3.34, actual.First().Point);
        }

        [TestMethod]
        public void GetTotalPoints_DifferentExercises()
        {
            var actual = ResultProcessor.GetTotalPoints(new[] { new AhkTaskResult() { TaskName = "a", ExerciseName = "ex1", Points = 1 }, new AhkTaskResult() { TaskName = "b", ExerciseName = "ex2", Points = 2.34 } });
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual("ex1", actual[0].Name);
            Assert.AreEqual(1, actual[0].Point);
            Assert.AreEqual("ex2", actual[1].Name);
            Assert.AreEqual(2.34, actual[1].Point);
        }
    }
}
