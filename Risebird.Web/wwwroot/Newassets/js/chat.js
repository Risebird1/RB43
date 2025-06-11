class ChatUI {
    constructor() {
        this.messages = [];
        this.isTyping = false;
        this.chatHistory = [];
        this.currentChatId = null;
        this.initialize();
    }

    initialize() {
        this.chatInput = document.querySelector('.chat-input');
        this.sendButton = document.querySelector('.send-button');
        this.messagesContainer = document.querySelector('.chat-messages');
        this.chatHistoryContainer = document.querySelector('.chat-history');
        this.newChatButton = document.querySelector('.new-chat-button');

        this.chatInput.addEventListener('input', () => this.handleInput());
        this.chatInput.addEventListener('keydown', (e) => this.handleKeyDown(e));
        this.sendButton.addEventListener('click', () => this.sendMessage());
        this.newChatButton.addEventListener('click', () => this.startNewChat());

        // Auto-resize textarea
        this.chatInput.addEventListener('input', () => {
            this.chatInput.style.height = 'auto';
            this.chatInput.style.height = this.chatInput.scrollHeight + 'px';
        });

        // Load chat history from localStorage
        this.loadChatHistory();
        
        // Start with a new chat if there's no active chat
        if (!this.currentChatId) {
            this.startNewChat(true);
        }

        // Add keyboard shortcut for new chat (Ctrl/Cmd + Shift + N)
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'N') {
                e.preventDefault();
                this.startNewChat();
            }
        });
    }

    loadChatHistory() {
        const savedHistory = localStorage.getItem('chatHistory');
        if (savedHistory) {
            this.chatHistory = JSON.parse(savedHistory);
            this.renderChatHistory();
        }

        const lastChatId = localStorage.getItem('currentChatId');
        if (lastChatId && this.chatHistory.find(c => c.id === lastChatId)) {
            this.loadChat(lastChatId);
        }
    }

    saveChatHistory() {
        // Keep only the last 25 chats (like ChatGPT)
        if (this.chatHistory.length > 25) {
            this.chatHistory = this.chatHistory.slice(0, 25);
        }
        localStorage.setItem('chatHistory', JSON.stringify(this.chatHistory));
        localStorage.setItem('currentChatId', this.currentChatId);
    }

    renderChatHistory() {
        this.chatHistoryContainer.innerHTML = '';
        
        // Group chats by date
        const groupedChats = this.groupChatsByDate();
        
        for (const [date, chats] of Object.entries(groupedChats)) {
            // Add date separator
            if (date !== 'Today' && date !== 'Yesterday' && date !== 'Previous 7 Days' && date !== 'Previous 30 Days') {
                const dateDiv = document.createElement('div');
                dateDiv.className = 'chat-history-date';
                dateDiv.textContent = date;
                this.chatHistoryContainer.appendChild(dateDiv);
            }

            // Add chats for this date
            chats.forEach(chat => {
                const chatItem = document.createElement('div');
                chatItem.className = `chat-history-item ${chat.id === this.currentChatId ? 'active' : ''}`;
                
                const chatIcon = document.createElement('div');
                chatIcon.className = 'chat-history-icon';
                chatIcon.innerHTML = `
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                            d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                    </svg>
                `;

                const chatText = document.createElement('div');
                chatText.className = 'chat-history-item-text';
                chatText.textContent = chat.title;

                const deleteButton = document.createElement('button');
                deleteButton.className = 'chat-history-delete';
                deleteButton.innerHTML = `
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                `;
                deleteButton.style.display = 'none';

                chatItem.appendChild(chatIcon);
                chatItem.appendChild(chatText);
                chatItem.appendChild(deleteButton);

                // Show/hide delete button on hover
                chatItem.addEventListener('mouseenter', () => {
                    deleteButton.style.display = 'flex';
                });
                chatItem.addEventListener('mouseleave', () => {
                    deleteButton.style.display = 'none';
                });

                // Delete chat
                deleteButton.addEventListener('click', (e) => {
                    e.stopPropagation();
                    this.deleteChat(chat.id);
                });

                // Load chat
                chatItem.addEventListener('click', () => {
                    if (this.currentChatId !== chat.id) {
                        this.loadChat(chat.id);
                    }
                });

                this.chatHistoryContainer.appendChild(chatItem);
            });
        }
    }

    groupChatsByDate() {
        const groups = {
            'Today': [],
            'Yesterday': [],
            'Previous 7 Days': [],
            'Previous 30 Days': [],
            'Older': []
        };

        const today = new Date();
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);

        this.chatHistory.forEach(chat => {
            const chatDate = new Date(parseInt(chat.id));
            const diffDays = Math.floor((today - chatDate) / (1000 * 60 * 60 * 24));

            if (chatDate.toDateString() === today.toDateString()) {
                groups['Today'].push(chat);
            } else if (chatDate.toDateString() === yesterday.toDateString()) {
                groups['Yesterday'].push(chat);
            } else if (diffDays <= 7) {
                groups['Previous 7 Days'].push(chat);
            } else if (diffDays <= 30) {
                groups['Previous 30 Days'].push(chat);
            } else {
                groups['Older'].push(chat);
            }
        });

        return groups;
    }

    startNewChat(isInitial = false) {
        if (!isInitial && this.currentChatId && this.messages.length === 0) {
            // If current chat is empty, just focus the input
            this.chatInput.focus();
            return;
        }

        const chatId = Date.now().toString();
        this.chatHistory.unshift({
            id: chatId,
            title: 'New chat',
            messages: [],
            created: new Date().toISOString()
        });

        this.loadChat(chatId);
        this.saveChatHistory();
        this.chatInput.focus();
    }

    loadChat(chatId) {
        this.currentChatId = chatId;
        const chat = this.chatHistory.find(c => c.id === chatId);
        if (chat) {
            this.messages = chat.messages;
            this.messagesContainer.innerHTML = '';
            this.messages.forEach(msg => this.addMessage(msg.content, msg.type));
            this.renderChatHistory();
            this.saveChatHistory();
            this.chatInput.focus();
        }
    }

    deleteChat(chatId) {
        const index = this.chatHistory.findIndex(c => c.id === chatId);
        if (index !== -1) {
            this.chatHistory.splice(index, 1);
            this.saveChatHistory();

            // If we deleted the current chat, load the next available chat or create a new one
            if (chatId === this.currentChatId) {
                if (this.chatHistory.length > 0) {
                    this.loadChat(this.chatHistory[0].id);
                } else {
                    this.startNewChat();
                }
            } else {
                this.renderChatHistory();
            }
        }
    }

    updateCurrentChatTitle(message) {
        const chat = this.chatHistory.find(c => c.id === this.currentChatId);
        if (chat && chat.title === 'New chat') {
            // Take the first 30 characters of the message, ending at the last complete word
            let title = message.substring(0, 50).split(' ').slice(0, -1).join(' ');
            if (title.length < message.length) title += '...';
            chat.title = title;
            this.renderChatHistory();
            this.saveChatHistory();
        }
    }

    handleInput() {
        const isEmpty = !this.chatInput.value.trim();
        this.sendButton.disabled = isEmpty;
    }

    handleKeyDown(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            this.sendMessage();
        }
    }

    async sendMessage() {
        const message = this.chatInput.value.trim();
        if (!message) return;

        // Add user message
        this.addMessageToChat(message, 'user');
        this.updateCurrentChatTitle(message);
        
        this.chatInput.value = '';
        this.chatInput.style.height = 'auto';
        this.handleInput();

        // Show typing indicator
        this.showTypingIndicator();

        try {
            // Make API call to your backend
            const response = await this.makeApiCall(message);
            
            // Remove typing indicator and add AI response
            this.hideTypingIndicator();
            this.addMessageToChat(response, 'ai');
        } catch (error) {
            this.hideTypingIndicator();
            this.addMessageToChat('Sorry, there was an error processing your request.', 'ai', true);
        }
    }

    addMessageToChat(content, type, isError = false) {
        this.addMessage(content, type, isError);
        
        // Update chat history
        const chat = this.chatHistory.find(c => c.id === this.currentChatId);
        if (chat) {
            chat.messages.push({ 
                content, 
                type,
                timestamp: new Date().toISOString()
            });
            this.saveChatHistory();
        }
    }

    addMessage(content, type, isError = false) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${type}-message`;

        const messageInner = document.createElement('div');
        messageInner.className = 'message-inner';

        const avatarDiv = document.createElement('div');
        avatarDiv.className = 'message-avatar';
        
        if (type === 'user') {
            avatarDiv.innerHTML = `
              <svg version="1.1" id="Icons" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 32 32" xml:space="preserve" width="25px" height="25px">
  <style type="text/css">
    .st0{fill:none;stroke:#3e6e8b;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;stroke-miterlimit:10;}
    .st1{fill:none;stroke:#3e6e8b;stroke-width:2;stroke-linejoin:round;stroke-miterlimit:10;}
  </style>
  <circle class="st0" cx="16" cy="16" r="14"></circle>
  <circle class="st0" cx="16" cy="13" r="5"></circle>
  <path class="st0" d="M5.4,25.1c1.8-4.1,5.8-7,10.6-7c4.8,0,8.9,2.9,10.6,7"></path>
</svg>`;
        } else {
            avatarDiv.innerHTML = `
                <svg width="25px" height="25px" viewBox="0 0 32 32" enable-background="new 0 0 32 32" id="_x3C_Layer_x3E_" version="1.1" xml:space="preserve" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
  <g id="emoji_x2C__chat_x2C__message_x2C__good_x2C__relationship_x2C__customer_relationship">
    <g id="XMLID_1827_">
      <path d="M19,11.521h2c0,0.55-0.45,1-1,1S19,12.07,19,11.521z" fill="#263238" id="XMLID_1851_"></path>

      <path d="M24.5,10c0.27,0,0.5,0.23,0.5,0.5S24.77,11,24.5,11S24,10.77,24,10.5S24.23,10,24.5,10z" fill="#10a37f" id="XMLID_1844_"></path>

      <path d="M15.5,10c0.27,0,0.5,0.23,0.5,0.5S15.77,11,15.5,11S15,10.77,15,10.5S15.23,10,15.5,10z" fill="#10a37f" id="XMLID_1843_"></path>
    </g>

    <g id="XMLID_258_">
      <g id="XMLID_4018_">
        <path d="     M21.5,21.52v1c0,1.66-1.34,3-3,3h-10l-3,4v-4h-1c-1.66,0-3-1.34-3-3v-10c0-1.659,1.34-3,3-3h3" fill="none" id="XMLID_4023_" stroke="#455A64" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M23.5,19.52l3,4v-4h1c1.66,0,3-1.34,3-3v-11c0-1.659-1.34-3-3-3h-15c-1.66,0-3,1.341-3,3v11c0,1.66,1.34,3,3,3H23.5z" fill="none" id="XMLID_4022_" stroke="#455A64" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M19,11.521h2c0,0.55-0.45,1-1,1S19,12.07,19,11.521z" fill="none" id="XMLID_4021_" stroke="#455A64" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M24.5,10c0.27,0,0.5,0.23,0.5,0.5S24.77,11,24.5,11S24,10.77,24,10.5S24.23,10,24.5,10z" fill="none" id="XMLID_4020_" stroke="#455A64" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M15.5,10c0.27,0,0.5,0.23,0.5,0.5S15.77,11,15.5,11S15,10.77,15,10.5S15.23,10,15.5,10z" fill="none" id="XMLID_4019_" stroke="#455A64" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>
      </g>

      <g id="XMLID_4012_">
        <path d="     M21.5,21.52v1c0,1.66-1.34,3-3,3h-10l-3,4v-4h-1c-1.66,0-3-1.34-3-3v-10c0-1.659,1.34-3,3-3h3" fill="none" id="XMLID_4017_" stroke="#263238" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M23.5,19.52l3,4v-4h1c1.66,0,3-1.34,3-3v-11c0-1.659-1.34-3-3-3h-15c-1.66,0-3,1.341-3,3v11c0,1.66,1.34,3,3,3H23.5z" fill="none" id="XMLID_4016_" stroke="#263238" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M19,11.521h2c0,0.55-0.45,1-1,1S19,12.07,19,11.521z" fill="none" id="XMLID_4015_" stroke="#263238" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M24.5,10c0.27,0,0.5,0.23,0.5,0.5S24.77,11,24.5,11S24,10.77,24,10.5S24.23,10,24.5,10z" fill="none" id="XMLID_4014_" stroke="#263238" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>

        <path d="     M15.5,10c0.27,0,0.5,0.23,0.5,0.5S15.77,11,15.5,11S15,10.77,15,10.5S15.23,10,15.5,10z" fill="none" id="XMLID_4013_" stroke="#263238" stroke-linecap="round" stroke-linejoin="round" stroke-miterlimit="10"></path>
      </g>
    </g>
  </g>
</svg>`;
        }

        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        
        if (isError) {
            contentDiv.style.color = '#ff4444';
        }

        // Handle code blocks
        const formattedContent = this.formatMessageContent(content);
        contentDiv.innerHTML = formattedContent;

        messageInner.appendChild(avatarDiv);
        messageInner.appendChild(contentDiv);
        messageDiv.appendChild(messageInner);

        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();

        // Add copy buttons to code blocks
        this.addCopyButtons(messageDiv);
    }

    formatMessageContent(content) {
        // Simple code block detection (you can enhance this based on your needs)
        return content.replace(/```([\s\S]*?)```/g, (match, code) => {
            return `<div class="code-block">
                        <pre><code>${this.escapeHtml(code.trim())}</code></pre>
                        <button class="copy-button">Copy</button>
                    </div>`;
        });
    }

    escapeHtml(unsafe) {
        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    addCopyButtons(messageElement) {
        const codeBlocks = messageElement.querySelectorAll('.code-block');
        codeBlocks.forEach(block => {
            const copyButton = block.querySelector('.copy-button');
            const codeContent = block.querySelector('code').textContent;

            copyButton.addEventListener('click', () => {
                navigator.clipboard.writeText(codeContent);
                copyButton.textContent = 'Copied!';
                setTimeout(() => {
                    copyButton.textContent = 'Copy';
                }, 2000);
            });
        });
    }

    showTypingIndicator() {
        if (this.isTyping) return;

        const typingDiv = document.createElement('div');
        typingDiv.className = 'message ai-message typing-message';
        typingDiv.innerHTML = `
            <div class="message-avatar">
                <img src="/img/ai-avatar.png" alt="AI Avatar">
            </div>
            <div class="typing-indicator">
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
            </div>
        `;

        this.messagesContainer.appendChild(typingDiv);
        this.scrollToBottom();
        this.isTyping = true;
    }

    hideTypingIndicator() {
        const typingMessage = document.querySelector('.typing-message');
        if (typingMessage) {
            typingMessage.remove();
        }
        this.isTyping = false;
    }

    scrollToBottom() {
        this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
    }

    async makeApiCall(message) {
        // Replace this with your actual API endpoint
        const response = await fetch('/api/aichat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ message })
        });

        if (!response.ok) {
            throw new Error('API call failed');
        }

        const data = await response.json();
        return data.response;
    }
}

// Initialize the chat UI when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    window.chatUI = new ChatUI();
}); 