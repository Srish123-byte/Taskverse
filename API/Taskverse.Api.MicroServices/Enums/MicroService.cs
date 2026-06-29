namespace Taskverse.Api.MicroServices.Enums;

public enum MicroService
{
    Auth = 5000,         // http port (launchSettings: http://localhost:5000)
    Users = 5009,        // http port (launchSettings: http://localhost:5009)
    ExamEngine = 5010,   // http port (launchSettings: http://localhost:5010)
    Proctor = 5011,      // http port (launchSettings: http://localhost:5011)
    CodingEngine = 5012, // http port (launchSettings: http://localhost:5012)
    Assessment = 5013,   // http port (launchSettings: http://localhost:5013)
    Reports = 5014,      // http port (launchSettings: http://localhost:5014)
    College = 5015       // http port (launchSettings: http://localhost:5015)
}
