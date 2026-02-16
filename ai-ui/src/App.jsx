import { useState } from 'react'

export default function App() {
const [messages, setMessages] = useState([])
  const [user, setUser] = useState('admin')
  const [pass, setPass] = useState('password')
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState('')
  const [citations, setCitations] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function login() {
    setError('')
    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: user, password: pass })
    })
    if (!res.ok) { setError('Login failed'); return }
    const data = await res.json()
    setToken(data.token)
  }

  async function ask() {
    setError('')
    setLoading(true)
    setAnswer('')
    setCitations([])
    const res = await fetch('/api/rag/ask', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ question })
    })
    setLoading(false)
    if (!res.ok) { setError('Ask failed'); return }
    setMessages(m => [...m, { role: 'user', text: question }])
    const data = await res.json()
    setMessages(m => [...m, { role: 'ai', text: data.answer, citations: data.citations }])
    setQuestion('')

    setAnswer(data.answer)
    setCitations(data.citations || [])
  }

  return (
    <div style={{ maxWidth: 900, margin: '40px auto', fontFamily: 'Arial' }}>
      <h2>AI RAG Demo</h2>

      {!token ? (
        <div style={{ padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
          <h3>Login</h3>
          <input value={user} onChange={e => setUser(e.target.value)} placeholder="Username" />
          <input type="password" value={pass} onChange={e => setPass(e.target.value)} placeholder="Password" style={{ marginLeft: 8 }} />
          <button onClick={login} style={{ marginLeft: 8 }}>Login</button>
        </div>
      ) : (
        <div style={{ padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <h3>Ask</h3>
            <button onClick={() => setToken('')}>Logout</button>
          </div>

          <textarea
            rows={4}
            style={{ width: '100%' }}
            value={question}
            onChange={e => setQuestion(e.target.value)}
            placeholder="Ask a question..."
          />
          <button onClick={ask} disabled={!question || loading} style={{ marginTop: 8 }}>
            {loading ? 'Thinking…' : 'Ask'}
          </button>

          {answer && (
            <>
              <h4>Answer</h4>
              <div style={{ whiteSpace: 'pre-wrap' }}>{answer}</div>

              <h4>Sources</h4>
              <ul>
                {citations.map(c => (
                  <li key={c.id}>
                    <div><b>#{c.id}</b> (distance: {c.distance?.toFixed?.(3) ?? c.distance})</div>
                    <div>{c.snippet}</div>
                  </li>
                ))}
              </ul>
            </>
          )}
        </div>
      )}

      {error && <p style={{ color: 'crimson' }}>{error}</p>}
    </div>
  )
}
