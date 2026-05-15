import { useState, useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import './App.css'
import { ApplicationState } from './types'
import type { JobApplication } from './types'
import ReviewModal from './ReviewModal'
import AddApplicationModal from './AddApplicationModal'
import SearchSettings from './SearchSettings'
import ProfileModal from './ProfileModal'

const API_BASE_URL = 'http://localhost:5053/api'
const HUB_URL = 'http://localhost:5053/applicationHub'

function App() {
  const [applications, setApplications] = useState<JobApplication[]>([])
  const [loading, setLoading] = useState(true)
  const [reviewingApp, setReviewingApp] = useState<JobApplication | null>(null)
  const [showAddModal, setShowAddModal] = useState(false)
  const [showSearchSettings, setShowSearchSettings] = useState(false)
  const [showProfileModal, setShowProfileModal] = useState(false)

  useEffect(() => {
    fetchApplications()

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build()

    connection.on("ApplicationUpdated", (id: string) => {
      console.log(`Application ${id} updated, refreshing...`)
      fetchApplications()
    })

    connection.start().catch(err => console.error('SignalR Error:', err))

    return () => {
      connection.stop()
    }
  }, [])

  const fetchApplications = async () => {
    try {
      console.log('Fetching applications from:', `${API_BASE_URL}/applications`)
      const response = await fetch(`${API_BASE_URL}/applications`)
      if (response.ok) {
        const data = await response.json()
        console.log('Raw API Response:', data)
        
        // Handle both wrapped { value: [] } and raw [...] responses
        const appList = Array.isArray(data) ? data : (data.value || [])
        setApplications(appList)
        console.log('Applications state updated:', appList)
      } else {
        console.error('API Error Response:', response.status, response.statusText)
      }
    } catch (error) {
      console.error('NETWORK ERROR:', error)
    } finally {
      setLoading(false)
    }
  }

  const columns: ApplicationState[] = [
    ApplicationState.Discovered,
    ApplicationState.Analyzing,
    ApplicationState.Tailoring,
    ApplicationState.FillingForm,
    ApplicationState.AwaitingManualApproval,
    ApplicationState.Submitted,
    ApplicationState.Failed
  ]

  const normalizeState = (state: any): string => {
    const stateMap: Record<number, string> = {
      0: ApplicationState.Discovered,
      1: ApplicationState.Analyzing,
      2: ApplicationState.Tailoring,
      3: ApplicationState.FillingForm,
      4: ApplicationState.AwaitingManualApproval,
      5: ApplicationState.Submitted,
      6: ApplicationState.Failed
    };
    return typeof state === 'number' ? stateMap[state] : state;
  }

  const getApplicationsByState = (columnState: ApplicationState) => {
    return applications.filter(app => {
      const appState = normalizeState(app.state);
      return appState?.toLowerCase() === columnState.toLowerCase();
    });
  }

  const processApplication = async (id: string) => {
    try {
      await fetch(`${API_BASE_URL}/applications/${id}/process`, { method: 'POST' })
    } catch (error) {
      console.error('Failed to trigger process:', error)
    }
  }

  const approveApplication = async (id: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/applications/${id}/approve`, { method: 'POST' })
      if (response.ok) {
        setReviewingApp(null)
        fetchApplications()
      } else {
        const msg = await response.text()
        alert(msg)
      }
    } catch (error) {
      console.error('Failed to approve:', error)
    }
  }

  const checkConnection = async () => {
    try {
      const res = await fetch(`${API_BASE_URL.replace('/api', '/health')}`)
      const data = await res.json()
      alert(`Backend Connection: ${data.status} at ${data.time}`)
    } catch (e) {
      alert('FAILED to reach backend. Check if Docker API container is running on port 5053.')
    }
  }

  return (
    <div className="app-container">
      <header>
        <h1>Job Application Assistant ({applications.length} Found)</h1>
        <div className="actions">
          <button onClick={checkConnection} style={{ backgroundColor: '#ff9800' }}>📡 Check API</button>
          <button onClick={() => setShowSearchSettings(true)} className="secondary">⚙️ Search Settings</button>
          <button onClick={() => setShowAddModal(true)}>+ New Application</button>
          <button className="secondary" onClick={() => setShowProfileModal(true)}>👤 User Profile</button>
        </div>
      </header>

      {loading ? (
        <div style={{ padding: '2rem' }}>Loading applications...</div>
      ) : (
        <div className="kanban-board">
          {columns.map(column => (
            <div key={column} className="kanban-column">
              <h2>{column}</h2>
              <div className="kanban-cards">
                {getApplicationsByState(column).map(app => (
                  <div key={app.id} className="kanban-card">
                    <h3>{app.jobTitle}</h3>
                    <p>{app.companyName}</p>
                    <div style={{ marginTop: '0.5rem', fontSize: '0.75rem', opacity: 0.8 }}>
                      {new Date(app.createdAt).toLocaleDateString()}
                    </div>
                    
                    {([ApplicationState.Analyzing, ApplicationState.Tailoring, ApplicationState.FillingForm] as string[]).includes(normalizeState(app.state)) && (
                      <div style={{ marginTop: '0.5rem', color: '#0052cc', fontSize: '0.8rem', fontWeight: 'bold' }}>
                        ⚡ Agent is working...
                      </div>
                    )}

                    {normalizeState(app.state) === ApplicationState.Discovered && (
                      <button 
                        style={{ marginTop: '0.5rem', width: '100%', padding: '0.25rem' }} 
                        onClick={() => processApplication(app.id)}
                      >
                        Start Automation
                      </button>
                    )}
                    
                    {normalizeState(app.state) === ApplicationState.Failed && (
                      <button 
                        style={{ marginTop: '0.5rem', width: '100%', padding: '0.25rem', backgroundColor: '#666' }} 
                        onClick={() => processApplication(app.id)}
                      >
                        Retry Automation
                      </button>
                    )}
                    {normalizeState(app.state) === ApplicationState.AwaitingManualApproval && (
                      <button 
                        style={{ marginTop: '0.5rem', width: '100%', padding: '0.25rem', backgroundColor: '#36b37e' }} 
                        onClick={() => setReviewingApp(app)}
                      >
                        Review & Approve
                      </button>
                    )}
                    {normalizeState(app.state) === ApplicationState.Submitted && (
                      <button 
                        style={{ marginTop: '0.5rem', width: '100%', padding: '0.25rem', backgroundColor: '#0052cc' }} 
                        onClick={() => setReviewingApp(app)}
                      >
                        View Audit Trail
                      </button>
                    )}
                  </div>
                ))}
                {getApplicationsByState(column).length === 0 && (
                  <div style={{ fontSize: '0.8rem', color: '#999', textAlign: 'center', marginTop: '1rem' }}>
                    No applications
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {reviewingApp && (
        <ReviewModal 
          application={reviewingApp} 
          onClose={() => setReviewingApp(null)} 
          onApprove={approveApplication}
        />
      )}

      {showAddModal && (
        <AddApplicationModal 
          onClose={() => setShowAddModal(false)} 
          onAdded={fetchApplications}
        />
      )}

      {showSearchSettings && (
        <SearchSettings 
          onClose={() => setShowSearchSettings(false)} 
          onScoutComplete={fetchApplications}
        />
      )}

      {showProfileModal && (
        <ProfileModal 
          onClose={() => setShowProfileModal(false)} 
        />
      )}
    </div>
  )
}

export default App
