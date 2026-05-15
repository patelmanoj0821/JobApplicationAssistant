import React, { useState, useEffect } from 'react';
import type { UserProfile } from './types';

const API_BASE_URL = 'http://localhost:5053/api';

const ProfileModal: React.FC<{ onClose: () => void }> = ({ onClose }) => {
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        fetchProfile();
    }, []);

    const fetchProfile = async () => {
        try {
            const res = await fetch(`${API_BASE_URL}/profiles`);
            const data = await res.json();
            if (data.length > 0) {
                setProfile(data[0]);
            }
        } catch (error) {
            console.error('Failed to fetch profile:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!profile) return;

        setSaving(true);
        try {
            await fetch(`${API_BASE_URL}/profiles/${profile.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(profile)
            });
            onClose();
        } catch (error) {
            console.error('Failed to save profile:', error);
        } finally {
            setSaving(false);
        }
    };

    const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (event) => {
            const text = event.target?.result as string;
            setProfile(prev => prev ? { ...prev, baseResumeText: text } : null);
        };
        reader.readAsText(file);
    };

    if (loading) {
        return (
            <div className="modal-overlay" style={{
                position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
                backgroundColor: 'rgba(0,0,0,0.7)', display: 'flex',
                justifyContent: 'center', alignItems: 'center', zIndex: 1000
            }}>
                <div style={{ backgroundColor: 'white', padding: '2rem', borderRadius: '8px' }}>
                    Loading profile data...
                </div>
            </div>
        );
    }

    return (
        <div className="modal-overlay" style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.7)', display: 'flex',
            justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
            <form onSubmit={handleSave} className="modal-content" style={{
                backgroundColor: 'white', padding: '2rem', borderRadius: '8px',
                width: '600px', maxWidth: '90%', maxHeight: '90vh', overflowY: 'auto',
                display: 'flex', flexDirection: 'column', gap: '1rem'
            }}>
                <h2>User Profile & Resume</h2>
                <p style={{ fontSize: '0.9rem', color: '#666' }}>This information is used by Gemini to tailor your applications.</p>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                        <label style={{ fontWeight: 'bold' }}>Full Name</label>
                        <input 
                            value={profile?.fullName || ''} 
                            onChange={e => setProfile(prev => prev ? { ...prev, fullName: e.target.value } : null)}
                            required 
                            style={{ padding: '0.5rem' }}
                        />
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                        <label style={{ fontWeight: 'bold' }}>Email Address</label>
                        <input 
                            value={profile?.email || ''} 
                            onChange={e => setProfile(prev => prev ? { ...prev, email: e.target.value } : null)}
                            type="email"
                            required 
                            style={{ padding: '0.5rem' }}
                        />
                    </div>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <label style={{ fontWeight: 'bold' }}>LinkedIn URL</label>
                    <input 
                        value={profile?.linkedInUrl || ''} 
                        onChange={e => setProfile(prev => prev ? { ...prev, linkedInUrl: e.target.value } : null)}
                        placeholder="https://linkedin.com/in/yourprofile"
                        style={{ padding: '0.5rem' }}
                    />
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <label style={{ fontWeight: 'bold' }}>Base Resume (Plain Text)</label>
                        <label className="secondary" style={{ 
                            fontSize: '0.8rem', cursor: 'pointer', color: '#0052cc', textDecoration: 'underline' 
                        }}>
                            Upload .txt file
                            <input type="file" accept=".txt,.md" onChange={handleFileUpload} style={{ display: 'none' }} />
                        </label>
                    </div>
                    <textarea 
                        value={profile?.baseResumeText || ''} 
                        onChange={e => setProfile(prev => prev ? { ...prev, baseResumeText: e.target.value } : null)}
                        required
                        placeholder="Paste your full resume here..."
                        style={{ width: '100%', height: '300px', padding: '0.5rem', fontFamily: 'monospace', fontSize: '0.85rem' }}
                    />
                </div>

                <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end', marginTop: '1rem' }}>
                    <button type="button" className="secondary" onClick={onClose}>Cancel</button>
                    <button type="submit" disabled={saving}>
                        {saving ? 'Saving...' : 'Save Profile'}
                    </button>
                </div>
            </form>
        </div>
    );
};

export default ProfileModal;
