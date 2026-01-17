import { useState } from 'react'
import Login from './Login'
import Register from './Register'
import Chat from './Chat'

function App() {
  const [mode,setMode] = useState<'login'|'register' | 'chat'>('login');
  if (mode === 'chat') return <Chat />
  return (
    <div style={{padding: 24}}>
      {mode === 'login' ? (
        <>
          <Login onSuccess={() => setMode('chat')} />
          <p>
            No Account? <button onClick={() => setMode('register')}>Register</button>
          </p>
        </>
      ): (
        <>
        <Register onDone={()=> setMode('login')} />
        <p>
          Already have an Account? <button onClick={() => setMode('login')}>Login</button>
        </p>
        </>
      )}
      
    </div>
  )
}

export default App
