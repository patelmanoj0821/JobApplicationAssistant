import React, { useState, useEffect } from 'react';

interface SearchCriteria {
    id: string;
    targetJobTitle: string;
    preferredLocation: string;
    isActive: boolean;
}

const API_BASE_URL = 'http://localhost:5053/api';

const SearchSettings: React.FC<{ onClose: () => void, onScoutComplete: () => void }> = ({ onClose, onScoutComplete }) => {
    const [criteria, setCriteria] = useState<SearchCriteria[]>([]);
    const [newTitle, setNewTitle] = useState('');
    const [newLocation, setNewLocation] = useState('United States');
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        fetchCriteria();
    }, []);

    const fetchCriteria = async () => {
        try {
            const res = await fetch(`${API_BASE_URL}/criteria`);
            const data = await res.json();
            setCriteria(data);
        } catch (error) {
            console.error('Failed to fetch criteria:', error);
        }
    };

    const addCriteria = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            await fetch(`${API_BASE_URL}/criteria`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ targetJobTitle: newTitle, preferredLocation: newLocation, isActive: true })
            });
            setNewTitle('');
            fetchCriteria();
        } catch (error) {
            console.error('Failed to add criteria:', error);
        }
    };

    const deleteCriteria = async (id: string) => {
        try {
            await fetch(`${API_BASE_URL}/criteria/${id}`, { method: 'DELETE' });
            fetchCriteria();
        } catch (error) {
            console.error('Failed to delete criteria:', error);
        }
    };

    const runScout = async () => {
        setSaving(true);
        try {
            await fetch(`${API_BASE_URL}/criteria/run-scout`, { method: 'POST' });
            onScoutComplete();
        } catch (error) {
            console.error('Failed to run scout:', error);
        } finally {
            setSaving(false);
            onClose();
        }
    };

    return (
        <div className="modal-overlay" style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.7)', display: 'flex',
            justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
            <div className="modal-content" style={{
                backgroundColor: 'white', padding: '2rem', borderRadius: '8px',
                width: '500px', display: 'flex', flexDirection: 'column', gap: '1.5rem'
            }}>
                <h2>Search Settings</h2>
                <p style={{ fontSize: '0.9rem', color: '#666' }}>The Scout will hunt for these roles every 12 hours.</p>

                <form onSubmit={addCriteria} style={{ display: 'flex', gap: '0.5rem' }}>
                    <input 
                        placeholder="Job Title (e.g. Engineering Manager)" 
                        value={newTitle} 
                        onChange={e => setNewTitle(e.target.value)} 
                        required 
                        style={{ flex: 2, padding: '0.5rem' }} 
                    />
                    <input 
                        placeholder="Location" 
                        value={newLocation} 
                        onChange={e => setNewLocation(e.target.value)} 
                        style={{ flex: 1, padding: '0.5rem' }} 
                    />
                    <button type="submit">Add</button>
                </form>

                <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
                    {criteria.map(c => (
                        <div key={c.id} style={{ 
                            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                            padding: '0.75rem', borderBottom: '1px solid #eee'
                        }}>
                            <div>
                                <div style={{ fontWeight: 'bold' }}>{c.targetJobTitle}</div>
                                <div style={{ fontSize: '0.8rem', color: '#888' }}>{c.preferredLocation}</div>
                            </div>
                            <button 
                                onClick={() => deleteCriteria(c.id)} 
                                style={{ backgroundColor: '#ff5252', padding: '0.25rem 0.5rem', fontSize: '0.8rem' }}
                            >
                                Remove
                            </button>
                        </div>
                    ))}
                </div>

                <div style={{ display: 'flex', gap: '1rem', justifyContent: 'space-between', marginTop: '1rem' }}>
                    <button onClick={runScout} disabled={saving} style={{ backgroundColor: '#36b37e' }}>
                        {saving ? 'Scouting...' : '🚀 Run Scout Now'}
                    </button>
                    <button className="secondary" onClick={onClose}>Close</button>
                </div>
            </div>
        </div>
    );
};

export default SearchSettings;
