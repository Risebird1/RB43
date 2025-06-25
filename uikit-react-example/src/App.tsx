import { App as SendbirdApp } from '@sendbird/uikit-react';
import '@sendbird/uikit-react/dist/index.css';

function App() {
  return (
    
    <div style={{ width:'100vw', height:'100vh' }}>
      {/* 
        This super smart component serves as a drop-in chat solution
        
        For advanced 🚀 customizations, use SendbirdProvider:
        https://sendbird.com/docs/chat/uikit/v3/react/essentials/sendbirdprovider
      */}
      <SendbirdApp
        appId={'4E9CEA3D-16D9-4D71-BD30-5BD199C8E7ED'}
        userId={'1432'}
        accessToken={'ACCESS_TOKEN'} // Optional, but recommended
      />
    </div>
  )
}

export default App;