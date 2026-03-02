import "./App.css";
import CanvasComponent from "./CanvasComponent.jsx";

function App() {
  return (
    <>
      <h1>Ghost Tracking Map</h1>
      <p>Ghosts are running across town. Watch out!<br/>Click on a ghost to see more details.</p>
      <CanvasComponent></CanvasComponent>
    </>
  );
}

export default App;
