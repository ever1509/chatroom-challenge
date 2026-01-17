import React from 'react'
import {apiPost} from './api';

export default function Login({ onSuccess}: { onSuccess: () => void }) {
    const [email, setEmail] = React.useState('');
    const [password, setPassword] = React.useState('');
    const [error, setError] = React.useState<string | null>(null);

    async function submit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);
        try {
            await apiPost('/login?useCookies=true', { email, password });
            onSuccess();
        } catch (err: any) {
            setError(err.message ?? 'Login failed');
        }
    }
  return (
    <div>
      <form onSubmit={submit} style={{display: 'grid', gap: 8, maxWidth: 360}}>
        <h2>Login</h2>
        <input placeholder='email' value={email} onChange={e => setEmail(e.target.value)} />
        <input placeholder='password' type='password' value={password} onChange={e => setPassword(e.target.value)} />
        {error && <div style={{color: 'crimson'}}>{error}</div>}
        <button type='submit'>Login</button>
      </form>
    </div>
  )
}
