from flask import Flask, request, jsonify
import joblib
import pandas as pd
import os

app = Flask(__name__)

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

features = joblib.load(os.path.join(BASE_DIR, 'models', 'social_media_features.pkl'))
model    = joblib.load(os.path.join(BASE_DIR, 'models', 'social_media_xgb.pkl'))

@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok'})

@app.route('/score-post', methods=['POST'])
def score_post():
    post_details = request.get_json()
    if not post_details:
        return jsonify({'error': 'No JSON body provided'}), 400

    try:
        input_df = pd.DataFrame([post_details])
        input_encoded = pd.get_dummies(input_df)
        input_aligned = input_encoded.reindex(columns=features, fill_value=0)

        prob = float(model.predict_proba(input_aligned)[0][1])

        if prob >= 0.70:
            likelihood = 'High'
        elif prob >= 0.45:
            likelihood = 'Medium'
        else:
            likelihood = 'Low'

        recommendations = []
        if not post_details.get('features_resident_story', False):
            recommendations.append(
                'Consider featuring a resident story — this is the single strongest driver of donation referrals'
            )
        if post_details.get('post_type') not in ['ImpactStory', 'FundraisingAppeal']:
            recommendations.append(
                'ImpactStory and FundraisingAppeal post types convert significantly better than other types'
            )
        if post_details.get('sentiment_tone') in ['Informative', 'Grateful']:
            recommendations.append(
                'Emotional, Urgent, or Celebratory tone drives more conversions than Informative or Grateful'
            )

        return jsonify({
            'conversion_likelihood': likelihood,
            'conversion_probability': round(prob, 3),
            'recommendations': recommendations
        })

    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5050, debug=True)