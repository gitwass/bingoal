import { Component, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { WebsocketService } from "./services/websocket.service";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements AfterViewInit {
  @ViewChild('canvas1', { static: false }) canvas1Ref!: ElementRef<HTMLCanvasElement>;
  @ViewChild('canvas2', { static: false }) canvas2Ref!: ElementRef<HTMLCanvasElement>;

  private ctx1!: any;
  private ctx2!: any;
  private isDrawing: boolean = false;

  constructor(private websocketService: WebsocketService) {}

  ngAfterViewInit(): void {
    this.initializeCanvas();

    this.canvas1Ref.nativeElement.addEventListener('mousedown', this.startDrawing.bind(this));
    this.canvas1Ref.nativeElement.addEventListener('mousemove', this.draw.bind(this));
    this.canvas1Ref.nativeElement.addEventListener('mouseup', this.stopDrawing.bind(this));
    this.canvas1Ref.nativeElement.addEventListener('mouseup', this.sendMsg.bind(this));
    this.canvas1Ref.nativeElement.addEventListener('mouseout', this.stopDrawing.bind(this));

    this.websocketService.onMessage((message: string) => {
      console.log("RECEIVED MESSAGE");
      this.drawImageCanvas2(this.stringToBlob(message));
    });
  }


  private startDrawing(e: MouseEvent): void {
    this.isDrawing = true;
    this.draw(e);
  }

  private stopDrawing(): void {
    this.isDrawing = false;
    this.ctx1.beginPath();
  }

  public initializeCanvas(): void {
    const canvas1 = this.canvas1Ref.nativeElement;
    const canvas2 = this.canvas2Ref.nativeElement;

    this.ctx1 = canvas1.getContext('2d');
    this.ctx2 = canvas2.getContext('2d');

    this.ctx1.clearRect(0, 0, canvas1.width, canvas1.height);
    this.ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
  }


  private draw(e: MouseEvent): void {
    if (!this.isDrawing) return;

    const rect = this.canvas1Ref.nativeElement.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    this.ctx1.lineTo(x, y);
    this.ctx1.stroke();
  }

  private sendMsg(): void {
    const canvasDataUrl = this.canvas1Ref.nativeElement.toDataURL('image/png');
    this.websocketService.send(this.imageToBlob(canvasDataUrl));
    console.log('Datos del canvas1:', canvasDataUrl);
  }

  private drawImageCanvas2(blob: Blob): void {
    const canvas = this.canvas2Ref.nativeElement;
    const ctx = canvas.getContext('2d');

    const img = new Image();
    img.onload = () => {
      ctx?.drawImage(img, 0, 0, canvas.width, canvas.height);
    };
    img.src = URL.createObjectURL(blob);
  }

  private stringToBlob(text: string): Blob {
    return new Blob([text], { type: 'image/png' });
  }

  private imageToBlob(image: any): Blob {
    const binaryData = atob(image.split(',')[1]);
    const arrayBuffer = new ArrayBuffer(binaryData.length);
    const byteArray = new Uint8Array(arrayBuffer);

    for (let i = 0; i < binaryData.length; i++) {
      byteArray[i] = binaryData.charCodeAt(i);
    }

    const blob = new Blob([arrayBuffer], { type: 'image/png' });
    return blob;
  }
}
