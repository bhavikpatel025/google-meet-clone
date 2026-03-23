export interface ChatMessage {
  id: number;
  senderId: string;
  senderName: string;
  message: string;
  sentAt: Date;
  type: string;
}

export interface SendMessageRequest {
  message: string;
}