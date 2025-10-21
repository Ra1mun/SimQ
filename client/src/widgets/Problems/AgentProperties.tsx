import { ComponentType } from 'react';
import { Modal, Box, Typography, Button } from '@mui/material';

import './Problems.scss';


type AgentPropertiesModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onSave: () => void;
}

const style = {
  position: 'absolute',
  top: '50%',
  left: '50%',
  transform: 'translate(-50%, -50%)',
  width: 400,
  bgcolor: 'background.paper',
  border: '2px solid #000',
  boxShadow: 24,
  p: 4,
};

export const AgentPropertiesModal: ComponentType<AgentPropertiesModalProps> =
    ({ isOpen, onClose, onSave }) =>
{
    return <Modal
        open={isOpen}
        onClose={onClose}
        aria-labelledby="modal-modal-title"
        aria-describedby="modal-modal-description"
    >
        <Box sx={style}>
            <Typography id="modal-modal-title" variant="h6" component="h2">
                Text in a modal
            </Typography>
            <Typography id="modal-modal-description" sx={{ mt: 2 }}>
                Duis mollis, est non commodo luctus, nisi erat porttitor ligula.
            </Typography>
            <Button onClick={onSave}>Save</Button>
        </Box>
    </Modal>;
};
