import { useState } from 'react'
import { apiPost } from './api'

export default function Register({ onDone }: { onDone: () => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await apiPost('/register', { email, password })
      onDone()
    } catch (err: any) {
      setError(err.message ?? 'Register failed')
    }
  }

  return (
    <form onSubmit={submit} style={{ display: 'grid', gap: 8, maxWidth: 360 }}>
      <h2>Register</h2>
      <input placeholder="email" value={email} onChange={e => setEmail(e.target.value)} />
      <input placeholder="password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
      <button type="submit">Create account</button>
      {error && <div style={{ color: 'crimson' }}>{error}</div>}
    </form>
  )
}
