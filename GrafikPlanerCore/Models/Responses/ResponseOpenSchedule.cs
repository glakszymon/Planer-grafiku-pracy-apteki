namespace GrafikPlanerCore.Models.Responses;

public class ResponseOpenSchedule
{
    public string Status { get; set; }
    public string Message { get; set; }
    public List<ScheduleRow>? Data { get; set; }
    
    public ResponseOpenSchedule(string status, string message,  List<ScheduleRow>? data =  null)
    {
        Status = status;
        Message = message;
        Data = data;
    }
}