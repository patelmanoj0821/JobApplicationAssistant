import React, { useState, useEffect } from 'react';
import type { UserProfile } from './types';

interface AddApplicationModalProps {
    onClose: () => void;
    onAdded: () => void;
}

const API_BASE_URL = 'http://localhost:5053/api';

const AddApplicationModal: React.FC<AddApplicationModalProps> = ({ onClose, onAdded }) => {
    const [companyName, setCompanyName] = useState('');
    const [jobTitle, setJobTitle] = useState('');
    const [jobPostingUrl, setJobPostingUrl] = useState('');
    const [profile, setProfile] = useState<UserProfile | null>(null);

    useEffect(() => {
        // Fetch the first profile to use as a default
        fetch(`${API_BASE_URL}/profiles`)
            .then(res => res.json())
            .then(data => {
                if (data.length > 0) setProfile(data[0]);
            });
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!profile) return;

        try {
            const response = await fetch(`${API_BASE_URL}/applications`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    companyName,
                    jobTitle,
                    jobPostingUrl,
                    userProfileId: profile.id,
                    state: 'Discovered'
                })
            });

            if (response.ok) {
                onAdded();
                onClose();
            }
        } catch (error) {
            console.error('Failed to add application:', error);
        }
    };

    return (
        <div className="modal-overlay" style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.7)', display: 'flex',
            justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
            <form className="modal-content" onSubmit={handleSubmit} style={{
                backgroundColor: 'white', padding: '2rem', borderRadius: '8px',
                width: '400px', display: 'flex', flexDirection: 'column', gap: '1rem'
            }}>
                <h2>Add New Application</h2>
                
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <label>Company Name</label>
                    <input value={companyName} onChange={e => setCompanyName(e.target.value)} required style={{ padding: '0.5rem' }} />
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <label>Job Title</label>
                    <input value={jobTitle} onChange={e => setJobTitle(e.target.value)} required style={{ padding: '0.5rem' }} />
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <label>Job Posting URL</label>
                    <input value={jobPostingUrl} onChange={e => setJobPostingUrl(e.target.value)} type="url" required style={{ padding: '0.5rem' }} />
                </div>

                <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end', marginTop: '1rem' }}>
                    <button type="button" className="secondary" onClick={onClose}>Cancel</button>
                    <button type="submit" disabled={!profile}>Add Application</button>
                </div>
            </form>
        </div>
    );
};

export default AddApplicationModal;
