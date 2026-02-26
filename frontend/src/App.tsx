import { BrowserRouter, Routes, Route, Link } from "react-router-dom";
import { AppBar, Toolbar, Button, Container } from "@mui/material";
import MappingEditorPage from "./pages/MappingEditorPage";
import SimulationPage from "./pages/SimulationPage";

export default function App() {
  return (
    <BrowserRouter>
      <AppBar position="static">
        <Toolbar>
          <Button color="inherit" component={Link} to="/">Mapping Editor</Button>
          <Button color="inherit" component={Link} to="/simulation">Simulation</Button>
        </Toolbar>
      </AppBar>
      <Container sx={{ py: 3 }}>
        <Routes>
          <Route path="/" element={<MappingEditorPage />} />
          <Route path="/simulation" element={<SimulationPage />} />
        </Routes>
      </Container>
    </BrowserRouter>
  );
}
