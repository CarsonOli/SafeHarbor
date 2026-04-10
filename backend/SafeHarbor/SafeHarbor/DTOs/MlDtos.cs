namespace SafeHarbor.DTOs;

public record DonorRiskFlagResponse(
    string   DonorId,
    string   DisplayName,
    string   Level,
    int      Score,
    string[] Signals);

public record ResidentReadinessFlagResponse(
    string   ResidentId,
    string   Level,
    int      Score,
    string   Action,
    string[] Signals);

public record SocialMediaScoreRequest(
    string Platform,
    string PostType,
    string MediaType,
    string ContentTopic,
    string SentimentTone,
    bool   FeaturesResidentStory,
    bool   HasCallToAction,
    string CallToActionType,
    bool   IsBoosted,
    double BoostBudgetPhp,
    int    PostHour,
    string DayOfWeek,
    int    NumHashtags,
    int    CaptionLength,
    int    MentionsCount);

public record SocialMediaScoreResponse(
    string   ConversionLikelihood,
    double   Probability,
    string[] Recommendations);
