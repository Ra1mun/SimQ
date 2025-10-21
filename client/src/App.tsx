import { useState } from 'react';
import { Tab, Tabs, Box } from '@mui/material'
import { TabContext } from '@mui/lab';

import { ProblemsTab, TasksTab, AgentsTab } from './widgets';
import { Pages } from './domain';

import './App.scss';


function App() {
  const [ currentPage, setPage ] = useState<Pages>( Pages.Problems )

  return (
    <div className = 'App'>
      <TabContext value = { currentPage }>
        <Box className = 'TabHeader'>
          <Tabs value={currentPage} onChange={ (_, value: Pages) => setPage(value) }>
            <Tab value = { Pages.Problems } label="Задачи"/>
            <Tab value = { Pages.Tasks } label="Процессы"/>
            <Tab value = { Pages.Agents } label="Агенты"/>
          </Tabs>
        </Box>
        <ProblemsTab tabId = { Pages.Problems }/>
        <TasksTab tabId = { Pages.Tasks }/>
        <AgentsTab tabId = { Pages.Agents }/>
      </TabContext>
    </div>
  );
}

export default App;
