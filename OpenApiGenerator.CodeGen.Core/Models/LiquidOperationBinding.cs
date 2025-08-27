namespace OpenApiGenerator.CodeGen.Core.Models;

public class LiquidOperationBinding
{
    public string Host { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string Path { get; set; }
    public string OperationName { get; set; }
    public string HttpMethod { get; set; }
    public ResolvedTypeInfo ResponseType { get; set; }
    public LiquidDtoBinding ResponseTypeBinding { get; set; }
    public ResolvedTypeInfo RequestType { get; set; }
    public LiquidDtoBinding RequestTypeBinding { get; set; }
    public LiquidPropertyBinding GetParameter { get; set; } 
    public List<LiquidPropertyBinding> Payload { get; set; }
    public string ApiName { get; set; }
    public string UserAgent { get; set; }
    public bool ForTests { get; set; }
    public string Version { get; set; }

    public LiquidOperationBinding Clone()
    {
        return new LiquidOperationBinding
        {
            Host = Host,
            Login = Login,
            Password = Password,
            Path = Path,
            OperationName = OperationName,
            HttpMethod = HttpMethod,
            ResponseType = ResponseType?.Clone(),
            RequestType = RequestType?.Clone(),
            GetParameter = GetParameter?.Clone(),
            Payload = Payload?.Select(x => x.Clone())?.ToList(),
            ApiName = ApiName,
            UserAgent = UserAgent
        };
    }
}