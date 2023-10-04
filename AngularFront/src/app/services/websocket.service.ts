import { Injectable } from '@angular/core';
import { webSocket, WebSocketSubject } from 'rxjs/webSocket'

@Injectable({
  providedIn: 'root'
})
export class WebsocketService {
  private socket: WebSocket;

   constructor() { 
    this.socket = new WebSocket('ws://localhost:8082/');
   this.socket.onopen = (event) => {
     console.log('WebSocket connection established');
   };

   this.socket.onerror = (event) => {
     console.error('WebSocket error:', event);
   };
   }

  sendMessage(message: string) {
    if (this.socket.readyState !== WebSocket.OPEN) {
      console.error('WebSocket connection is not open');
      return;
    }

    this.socket.send(message);
  }
  onMessage(callback: (message: any) => void) {
    this.socket.onmessage = (event) => {
      const message = event.data;
      callback(message);
      console.log(message);
    };
  }

  send(blob: Blob): void {
    if (this.socket.readyState === WebSocket.OPEN) {
      console.log(blob);
      this.socket.send(blob);
    } else {
      console.error('WebSocket connection is not open.');
    }
  }

  sendCanvasData(data: string) {
    if (this.socket.readyState !== WebSocket.OPEN) {
      console.error('WebSocket connection is not open');
      return;
    }

    this.socket.send(data);
  }
}
