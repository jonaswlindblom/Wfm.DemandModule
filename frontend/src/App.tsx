import TooeasyModule from "./TooeasyModule";
import TopBar from "./components/TopBar";

export default function App() {
  return (
    <div className="flex h-screen flex-col">
      <TopBar />
      <div className="min-h-0 flex-1">
        <TooeasyModule />
      </div>
    </div>
  );
}
