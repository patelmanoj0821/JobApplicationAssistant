import React from 'react';
import type { JobApplication } from './types';

interface ReviewModalProps {
    application: JobApplication;
    onClose: () => void;
    onApprove: (id: string) => void;
}

const ReviewModal: React.FC<ReviewModalProps> = ({ application, onClose, onApprove }) => {
    const isSubmitted = application.state === 'Submitted' || (application.state as any) === 5;

    // Find the latest log with a screenshot
    const latestLog = [...application.logs]
        .reverse()
        .find(l => l.screenshotLocalPath);

    return (
        <div className="modal-overlay" style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.7)', display: 'flex',
            justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
            <div className="modal-content" style={{
                backgroundColor: 'white', padding: '2rem', borderRadius: '8px',
                maxWidth: '90%', maxHeight: '90%', overflowY: 'auto',
                display: 'flex', flexDirection: 'column', gap: '1rem'
            }}>
                <h2>{isSubmitted ? '✅ Submission Audit' : 'Review Application'}: {application.companyName}</h2>
                <p><strong>Job Title:</strong> {application.jobTitle}</p>
                {isSubmitted && <p style={{ color: '#36b37e', fontWeight: 'bold' }}>This application was submitted on {new Date(application.submittedAt!).toLocaleString()}</p>}
                
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                        <h3>AI Tailored Content</h3>
                        <div>
                            <label style={{ fontWeight: 'bold' }}>Cover Letter:</label>
                            <textarea 
                                readOnly 
                                value={application.tailoredCoverLetter || 'No cover letter generated.'} 
                                style={{ width: '100%', height: '200px', marginTop: '0.5rem', padding: '0.5rem' }}
                            />
                        </div>
                        <div>
                            <label style={{ fontWeight: 'bold' }}>Resume Improvement Tips:</label>
                            <div style={{ 
                                backgroundColor: '#fff9db', 
                                padding: '1rem', 
                                borderLeft: '4px solid #fcc419',
                                marginTop: '0.5rem',
                                fontSize: '0.9rem'
                            }}>
                                {application.logs.find(l => l.message.includes('Resume Tips:'))?.message.split('Resume Tips:')[1] || 'No specific tips found.'}
                            </div>
                        </div>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                        <h3>{isSubmitted ? 'Final Confirmation' : 'Form Preview'}</h3>
                        {latestLog?.screenshotLocalPath ? (
                            <div style={{ border: '1px solid #ddd', overflow: 'hidden' }}>
                                <img 
                                    src={`http://localhost:5053${latestLog.screenshotLocalPath}`} 
                                    alt="Form Preview" 
                                    style={{ width: '100%', display: 'block' }}
                                />
                            </div>
                        ) : (
                            <div style={{ padding: '2rem', textAlign: 'center', backgroundColor: '#f9f9f9' }}>
                                No screenshot available.
                            </div>
                        )}
                    </div>
                </div>

                <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end', marginTop: '1rem' }}>
                    <button className="secondary" onClick={onClose}>{isSubmitted ? 'Close' : 'Cancel'}</button>
                    {!isSubmitted && <button onClick={() => onApprove(application.id)}>Approve & Submit</button>}
                </div>
            </div>
        </div>
    );
};

export default ReviewModal;
