using FaceRecReport.Enums;

namespace FaceRecReport.Entities;

public record Event(DateTime Timestamp,
    string? ExternalId,
    /*string ChannelId,*/
    string? ChannelName,
    bool IsIdentified,
    string? LastName,
    string? FirstName,
    string? Patronymic,
    string? Groups,
    string? AdditionalInfo,
    double Similarity,
    int Age,
    Gender Gender,
    string ImageBytes,
    Emotion Emotion,
    double EmotionConfidence)
{ }