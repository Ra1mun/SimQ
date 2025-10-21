import { ComponentType } from 'react';
import { TabPanel } from '@mui/lab';

import { Pages } from '../../domain';

type AgentsTabProps = {
  tabId: Pages;
}

export const AgentsTab: ComponentType<AgentsTabProps> = ({ tabId }) => {
  return <TabPanel value = { tabId } style={{ background: 'red' }}>
    AgentsTab
  </TabPanel>;
};
