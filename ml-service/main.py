"""
SafeHarbor ML Service — Social Media Post Scorer
Serves the XGBoost model trained in ml-pipelines/social-media-donation-conversion.ipynb
AUC: 0.898 on 812 social media posts (test set + 5-fold CV)
"""
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import joblib
import numpy as np
import pandas as pd
from pathlib import Path

app = FastAPI(title="SafeHarbor ML Service", version="1.0.0")

MODEL_DIR = Path(__file__).parent / "models"

# Load artifacts at startup — fail fast if missing
try:
    xgb_model = joblib.load(MODEL_DIR / "social_media_xgb.pkl")
    features  = joblib.load(MODEL_DIR / "social_media_features.pkl")
except FileNotFoundError as e:
    raise RuntimeError(
        f"Model artifacts missing from ml-service/models/. "
        f"Run ml-pipelines/social-media-donation-conversion.ipynb to generate them. "
        f"Missing: {e}"
    )


class PostScoreRequest(BaseModel):
    platform:               str
    post_type:              str
    media_type:             str
    content_topic:          str
    sentiment_tone:         str
    features_resident_story: bool
    has_call_to_action:     bool
    call_to_action_type:    str
    is_boosted:             bool
    boost_budget_php:       float
    post_hour:              int
    day_of_week:            str
    num_hashtags:           int
    caption_length:         int
    mentions_count:         int


class PostScoreResponse(BaseModel):
    conversion_likelihood: str        # "High" | "Medium" | "Low"
    probability:           float
    recommendations:       list[str]


@app.post("/score-post", response_model=PostScoreResponse)
def score_post(req: PostScoreRequest) -> PostScoreResponse:
    # Build numeric base row
    row: dict = {
        "num_hashtags":             req.num_hashtags,
        "caption_length":           req.caption_length,
        "mentions_count":           req.mentions_count,
        "boost_budget_php":         req.boost_budget_php,
        "post_hour":                req.post_hour,
        "has_call_to_action":       int(req.has_call_to_action),
        "features_resident_story":  int(req.features_resident_story),
        "is_boosted":               int(req.is_boosted),
    }

    # One-hot encode categoricals to match training columns exactly
    for col, val in [
        ("platform",          req.platform),
        ("post_type",         req.post_type),
        ("media_type",        req.media_type),
        ("content_topic",     req.content_topic),
        ("sentiment_tone",    req.sentiment_tone),
        ("call_to_action_type", req.call_to_action_type),
        ("day_of_week",       req.day_of_week),
    ]:
        col_name = f"{col}_{val}"
        if col_name in features:
            row[col_name] = 1

    df = pd.DataFrame([row]).reindex(columns=features, fill_value=0)
    prob = float(xgb_model.predict_proba(df)[0][1])

    likelihood = "High" if prob >= 0.65 else "Medium" if prob >= 0.40 else "Low"

    recs: list[str] = []
    if not req.features_resident_story:
        recs.append("Feature a resident story — strongest single driver of donation referrals")
    if req.sentiment_tone in ("Informative", "Grateful"):
        recs.append("Emotional or Urgent tone converts significantly better than Informative/Grateful")
    if req.media_type not in ("Reel", "Video"):
        recs.append("Video or Reel format outperforms static images for referral generation")
    if req.post_type not in ("ImpactStory", "FundraisingAppeal"):
        recs.append("ImpactStory and FundraisingAppeal post types drive the most conversions")
    if not recs:
        recs.append("Post looks well-optimized for conversion — publish during peak hours (10am Tuesday)")

    return PostScoreResponse(
        conversion_likelihood=likelihood,
        probability=round(prob, 3),
        recommendations=recs[:3],
    )


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "model": "social_media_xgb", "features": len(features)}
