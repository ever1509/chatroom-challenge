import React, { use, useEffect } from 'react'
import * as signalR from '@microsoft/signalr';
import { apiGet, apiPost } from './api';

type Room = { id: number; name: string };
type Msg = { id: number; roomId: number; userName: string; text: string; timeStamp: string; isBot: boolean };

export default function Chat() {
    const [rooms, setRooms] = React.useState<Room[]>([]);
    const [roomId, setRoomId] = React.useState<number | null>(null);
    const [messages, setMessages] = React.useState<Msg[]>([]);
    const [text, setText] = React.useState('');
    const [newRoomName, setNewRoomName] = React.useState('');
    const [error, setError] = React.useState<string | null>(null);

    const connectionRef = React.useRef<signalR.HubConnection | null>(null);

    async function loadRooms() {
        const data = await apiGet<Room[]>('/api/rooms');
        setRooms(data);
        if(!roomId && data.length > 0) setRoomId(data[0].id);
    }

    useEffect(() => {
        loadRooms().catch(err => setError(err.message ?? 'Failed to load rooms'));
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (roomId === null) return;
        apiGet<Msg[]>(`/api/rooms/${roomId}/messages`)
            .then(setMessages)
            .catch(err => setError(err.message ?? 'Failed to load messages'));
    }, [roomId]);

    async function ensureSignalR() {
        if (connectionRef.current) return connectionRef.current;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat', { withCredentials: true })
            .withAutomaticReconnect()
            .build();

        connection.on('newMessage', (msg: Msg) => {
            setMessages(prev => 
                { 
                    const next = [...prev, msg];
            // keep only last 50 messages
            return next.length > 50 ? next.slice(next.length - 50) : next;
        });
        });

        await connection.start();
        connectionRef.current = connection;
        return connection;
    }

    useEffect(() => {
        let active = true;
        (async () => {

            if( roomId === null) return;
            const connection = await ensureSignalR();
            if (!active) return;
            await connection.invoke('JoinRoom', roomId);

        })().catch(err => setError(err.message ?? 'Failed to connect to chat'));

        return () => {
            active = false;
            const connection = connectionRef.current;
            if (connection && roomId !== null) {
                connection.invoke('LeaveRoom', roomId).catch(() => { });
            }
        };
    }, [roomId]);

    async function sendMessage(){
        setError(null);
        if(!roomId) return;
        const msg = text.trim();
        if(!msg) return;

        try {
            const connection = await ensureSignalR();
            await connection.invoke('SendMessage', roomId, msg);
            setText('');
        } catch(err: any) {
            setError(err.message ?? 'Failed to send message');
        }   
    }

    async function createRoom(){
        setError(null);
        const name = newRoomName.trim();
        if(!name) return;
        try {
            await apiPost<Room>('/api/rooms', { name });
            setNewRoomName('');
            await loadRooms();
        } catch(err: any) {
            setError(err.message ?? 'Failed to create room');
        }
    }

    async function logout(){
        await apiPost('/logout', {});
        window.location.reload();
    }

  return (
    <div  style={{display: 'grid', gridTemplateColumns: '260px 1fr', gap:12, padding:16, }}>
      <div style={{borderRight: '1px solid #ddd', paddingRight: 12}}>
        <h3>Rooms</h3>
        <div style={{display: 'grid', gap:6}}>  
            {rooms.map(r => (
                <button
                    key={r.id}
                    onClick={() => setRoomId(r.id)}
                    style={{textAlign: 'left', fontWeight: r.id === roomId ? 'bold' : 'normal' }}
                >
                    {r.name}
                </button>
            ))}
        </div>

        <div style={{marginTop: 12, display:'grid', gap:6}}>
            <input placeholder='new room name' value={newRoomName} onChange={e=> setNewRoomName(e.target.value)} />
            <button onClick={createRoom}>Create Room</button>
            <button onClick={logout}>Logout</button>
        </div>
        {error && <div style={{color: 'crimson', marginTop: 12}}>{error}</div>}
      </div>

      <div style={{display: 'grid', gridTemplateRows: '1fr auto', gap: 10}}>
        <div style={{border: '1px solid #ddd', padding:12, overflow:'auto', height:'70vh'}}>
            {messages.map(m => (
                <div key={m.id} style={{marginBottom: 8, opacity: m.isBot ? 0.85 : 1}}>
                    <div style={{fontSize: 12, color: '#555'}}>
                        <b>{m.userName}</b> . {new Date(m.timeStamp).toLocaleString()}
                    </div>
                    <div>{m.text}</div>
                </div>
            ))}
        </div>

        <div style={{display: 'flex', gap: 8}}>
            <input
                style={{flex: 1}}
                placeholder='Type a message...'
                value={text}
                onChange={e => setText(e.target.value)}
                onKeyDown={e => e.key === 'Enter' ? sendMessage() : undefined}
            />
            <button onClick={sendMessage}>Send</button>
        </div>
      </div>
    </div>
  )
}
