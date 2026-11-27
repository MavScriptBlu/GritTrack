using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.App.Tests;

public class GpaCalculatorServiceTests
{
    private readonly GpaCalculatorService _calculator = new();

    [Theory]
    [InlineData("A", 4.0)]
    [InlineData("B+", 3.3)]
    [InlineData("c-", 1.7)]
    [InlineData("F", 0.0)]
    public void GetGradePoints_KnowsTheLetterGrades(string letter, double expected)
    {
        Assert.Equal(expected, _calculator.GetGradePoints(letter));
    }

    [Fact]
    public void CalculateGpa_WeighsCoursesByCredits()
    {
        var courses = new List<Course>
        {
            new() { Credits = 3, GradePoints = 4.0 },
            new() { Credits = 1, GradePoints = 2.0 }
        };

        Assert.Equal(3.5, _calculator.CalculateGpa(courses), 2);
    }

    [Fact]
    public void CalculateGpa_WithNoCourses_IsZero()
    {
        Assert.Equal(0, _calculator.CalculateGpa(new List<Course>()));
    }
}
