import { ComponentType } from 'react';
import { TabPanel } from '@mui/lab';

import { Pages } from '../../domain';

type TasksTabProps = {
  tabId: Pages;
}

export const TasksTab: ComponentType<TasksTabProps> = ({ tabId }) => {
  return <TabPanel value = { tabId } style={{ background: 'red' }}>
    TasksTab
  </TabPanel>;
};
