export const ApplicationState = {
    Discovered: "Discovered",
    Analyzing: "Analyzing",
    Tailoring: "Tailoring",
    FillingForm: "FillingForm",
    AwaitingManualApproval: "AwaitingManualApproval",
    Submitted: "Submitted",
    Failed: "Failed"
} as const;

export type ApplicationState = typeof ApplicationState[keyof typeof ApplicationState];

export interface JobApplication {
    id: string;
    companyName: string;
    jobTitle: string;
    jobPostingUrl: string;
    rawJobDescription?: string;
    tailoredCoverLetter?: string;
    tailoredResumePath?: string;
    state: ApplicationState;
    createdAt: string;
    submittedAt?: string;
    logs: ApplicationLog[];
}

export interface ApplicationLog {
    id: string;
    jobApplicationId: string;
    message: string;
    screenshotLocalPath?: string;
    timestamp: string;
    level: number;
}

export interface UserProfile {
    id: string;
    fullName: string;
    email: string;
    serializedResumeData?: string;
    linkedInUrl?: string;
    baseResumeText?: string;
}
