function addMessage(content, isUser) {
    // Creates and adds message elements to the chat
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${isUser ? 'user-message' : 'ai-message'}`;
    messageDiv.textContent = content;
    chatMessages.appendChild(messageDiv);
}

async function sendMessage() {
    // 1. Get user input
    const message = userInput.value.trim();
    
    // 2. Show user message
    addMessage(message, true);
    
    // 3. Show "AI is typing" indicator
    showTypingIndicator();
    
    // 4. Send to server and get response
    const response = await fetch('/Chat/SendMessage', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: message })
    });
    
    // 5. Display AI response
    const data = await response.json();
    addMessage(data.message, false);
}