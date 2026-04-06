export interface PrivacyPolicySection {
  heading: string
  paragraphs: string[]
}

export interface PrivacyPolicyContentSource {
  updatedOnIso: string
  sections: PrivacyPolicySection[]
}

// NOTE: Centralize policy copy so legal updates only touch a single source module.
export const privacyPolicyContent: PrivacyPolicyContentSource = {
  updatedOnIso: '2026-04-06',
  sections: [
    {
      heading: 'What we collect',
      paragraphs: [
        'SafeHarbor collects contact information, donation activity, and limited service-delivery information needed to operate programs.',
        'Sensitive data is processed for care and compliance workflows only and is never shared for advertising purposes.'
      ]
    },
    {
      heading: 'How we use your information',
      paragraphs: [
        'We use your data to deliver services, issue receipts, and meet legal reporting obligations.',
        'Authorized staff access is restricted by role-based policy controls and audited for accountability.'
      ]
    },
    {
      heading: 'Retention and your choices',
      paragraphs: [
        'Records are retained based on legal and operational requirements, then removed or de-identified when retention windows expire.',
        'You may request updates, corrections, or deletion where permitted by applicable law.'
      ]
    }
  ]
}
