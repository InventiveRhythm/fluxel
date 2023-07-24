namespace fluxel.Constants; 

public static class ResponseStrings {
    public static string NoToken => MissingHeader("token");
    public static string InvalidToken => "The provided token is invalid.";
    public static string TokenUserNotFound => "The user associated with the provided token was not found.";
    
    public static string InvalidBodyJson => "The provided body is not valid JSON.";
    public static string InvalidBodyMissingProperty(string property) => $"The provided body is missing the '{property}' property.";
    
    public static string MapNotFound => "No map with the provided ID was found.";
    public static string MapSetNotFound => "No mapset with the provided ID was found.";
    
    public static string UserNotFound => "No user with the provided ID was found.";
    
    public static string ScoreNotFound => "No score with the provided ID was found.";

    public static string MissingHeader(string header) => $"The '{header}' header is missing.";
    public static string InvalidHeader(string header) => $"The '{header}' header is invalid.";
    
    public static string MissingParameter(string parameter) => $"The parameter '{parameter}' is missing.";
    public static string InvalidParameter(string parameter, string type) => $"The parameter '{parameter}' is not a valid {type}.";
}