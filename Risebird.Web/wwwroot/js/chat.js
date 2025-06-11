class ChatUI {
    constructor() {
        this.messages = [];
        this.isTyping = false;
        this.chatHistory = [];
        this.currentChatId = null;
        this.icons = {
            user: `<svg class="user-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 11C14.2091 11 16 9.20914 16 7C16 4.79086 14.2091 3 12 3C9.79086 3 8 4.79086 8 7C8 9.20914 9.79086 11 12 11Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M6 21V19C6 17.9391 6.42143 16.9217 7.17157 16.1716C7.92172 15.4214 8.93913 15 10 15H14C15.0609 15 16.0783 15.4214 16.8284 16.1716C17.5786 16.9217 18 17.9391 18 19V21" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            ai: `<svg class="ai-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 3C7.02944 3 3 7.02944 3 12C3 16.9706 7.02944 21 12 21C16.9706 21 21 16.9706 21 12C21 7.02944 16.9706 3 12 3Z" stroke="#10a37f" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M9 10L12 7M12 7L15 10M12 7V17" stroke="#10a37f" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M7 14L12 17L17 14" stroke="#10a37f" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            error: `<svg class="error-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 8V12M12 16H12.01M21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C16.9706 3 21 7.02944 21 12Z" stroke="#ef4444" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            copy: `<svg class="copy-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M8 7V15C8 16.1046 8.89543 17 10 17H16M8 7V5C8 3.89543 8.89543 3 10 3H14.5858C14.851 3 15.1054 3.10536 15.2929 3.29289L19.7071 7.70711C19.8946 7.89464 20 8.149 20 8.41421V15C20 16.1046 19.1046 17 18 17H16M8 7H6C4.89543 7 4 7.89543 4 9V19C4 20.1046 4.89543 21 6 21H14C15.1046 21 16 20.1046 16 19V17" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            copied: `<svg class="copied-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M9 5H7C5.89543 5 5 5.89543 5 7V19C5 20.1046 5.89543 21 7 21H17C18.1046 21 19 20.1046 19 19V7C19 5.89543 18.1046 5 17 5H15M9 5C9 6.10457 9.89543 7 11 7H13C14.1046 7 15 6.10457 15 5M9 5C9 3.89543 9.89543 3 11 3H13C14.1046 3 15 3.89543 15 5M12 12L9 15M12 12L15 15M12 12V18" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            delete: `<svg class="delete-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M19 7L18.1327 19.1425C18.0579 20.1891 17.187 21 16.1378 21H7.86224C6.81296 21 5.94208 20.1891 5.86732 19.1425L5 7M10 11V17M14 11V17M15 7V4C15 3.44772 14.5523 3 14 3H10C9.44772 3 9 3.44772 9 4V7M4 7H20" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            newChat: `<svg class="new-chat-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 7V17M7 12H17M21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C16.9706 3 21 7.02944 21 12Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
            menu: `<svg class="menu-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M4 6H20M4 12H20M4 18H20" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`
        };
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
            let title = message.substring(0, 30).split(' ').slice(0, -1).join(' ');
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
        
        // Use the appropriate icon based on message type and error state
        avatarDiv.innerHTML = isError ? this.icons.error : (type === 'user' ? this.icons.user : this.icons.ai);

        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        
        if (isError) {
            contentDiv.style.color = '#ef4444';
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

            copyButton.innerHTML = this.icons.copy;
            
            copyButton.addEventListener('click', () => {
                navigator.clipboard.writeText(codeContent);
                copyButton.innerHTML = this.icons.copied;
                setTimeout(() => {
                    copyButton.innerHTML = this.icons.copy;
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