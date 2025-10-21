import { ComponentType, useState, useCallback, useEffect, useMemo } from 'react';
import { TabContext, TabPanel } from '@mui/lab';
import { Delete } from '@mui/icons-material';
import { List, ListItemButton, IconButton, ListItemText, Container, Box, Tab, Tabs, ListSubheader } from '@mui/material';

import { IProblem, Pages } from '../../domain';
import { ProblemsService } from '../../services';

import { AgentPropertiesModal } from './AgentProperties';
import { ProblemGraph } from './ProblemGraph';

import './Problems.scss';


type ProblemsTabProps = {
    tabId: Pages;
}

enum ProblemTab {
    Details,
    Results
}

export const ProblemsTab: ComponentType<ProblemsTabProps> = ({ tabId }) => {
    const [currentTab, setTab] = useState<ProblemTab>(ProblemTab.Details);
    const [currentProblem, setProblem] = useState<IProblem>();

    const a = useCallback(async () => await ProblemsService.loadProblems(), []);
    const selectProblem = useCallback(async (problemName: string) => {
        await ProblemsService.loadProblemInfo( problemName );

        setProblem(ProblemsService.problems.find( problem => problem.name === problemName ));
        }, []
    );

    a()
    console.log( ProblemsService.problems )

    const problemsList = useMemo( () => {
        return <List
            dense={false}
            subheader={
                <ListSubheader style = {{ backgroundColor: 'rgba(25, 118, 210, 0.2)' }}>
                    Список задач
                </ListSubheader>
            }
        >
            {ProblemsService.problems.map((problem, ind) =>
                <ListItemButton
                    key={problem.name}
                    aria-selected={currentProblem?.name === problem.name}
                    onClick={() => selectProblem(problem.name)}
                >
                    <ListItemText
                        primary={`${ind}. ${problem.name}`}
                    />
                </ListItemButton>
            )}
        </List>
    }, [ProblemsService.problems ]);

    return <TabPanel
        value={tabId}
        className='tab-container HorizontalFlex problems'
        sx={{ padding: 1 }}
    >
        <Container className='problems-list'>
            {problemsList}
        </Container>
        <Container>
            <TabContext value={currentTab}>
                <Box className='TabHeader'>
                    <Tabs value={currentTab} onChange={(_, value: ProblemTab) => setTab(value)}>
                        <Tab
                            value={ProblemTab.Details}
                            label="Детали"
                        />
                        <Tab
                            value={ProblemTab.Results}
                            label="Результаты моделирования"
                        />
                    </Tabs>
                </Box>
                <TabPanel
                    value={ProblemTab.Details}
                    className='tab-container HorizontalFlex'
                    sx = {{padding: 1}}
                >
                    <Container
                        className='problem-details'
                    >
                        <ProblemGraph problem = {currentProblem}/>
                    </Container>
                    <Container
                        className='VerticalFlex agent-properties'
                    >
                        Элементы системы
                    </Container>
                </TabPanel>
                <TabPanel
                    value={ProblemTab.Results}
                    // className = 'tab-container'
                    sx = {{padding: 1}}
                >
                </TabPanel>
            </TabContext>
        </Container>
    </TabPanel>;
};
