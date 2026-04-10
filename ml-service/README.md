# SafeHarbor ML Service

FastAPI sidecar serving the XGBoost social media post scorer (AUC 0.898).

## Setup
1. Run `ml-pipelines/social-media-donation-conversion.ipynb` top-to-bottom.
2. Copy the generated `.pkl` files into `ml-service/models/`:
   - social_media_xgb.pkl
   - social_media_features.pkl
3. `pip install -r requirements.txt`
4. `uvicorn main:app --reload`

## Endpoint
POST /score-post — accepts post details, returns conversion likelihood + recommendations.

## Environment variable
Set ML_SERVICE_BASE_URL in the .NET backend environment to point to this service.
