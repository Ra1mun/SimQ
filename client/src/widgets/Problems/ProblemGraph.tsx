import React, { Component, ComponentType, cloneElement, createRef } from 'react';
import * as d3 from 'd3'
import dagreD3, { GraphLabel } from 'dagre-d3'

import { TabContext, TabPanel } from '@mui/lab';
import { Delete } from '@mui/icons-material';
import { List, ListItemButton, IconButton, ListItemText, Container, Modal, Box, Typography, Button } from '@mui/material';

import { IProblem, Pages } from '../../domain';

import './Problems.scss';


type ProblemGraphProps = {
  problem?: IProblem;
};

type d3Node = {
	id: any
	label: string
	class?: string
	labelType?: labelType
	config?: object
}
type d3Link = {
	source: string
	target: string
	class?: string
	label?: string
	config?: object
}

let data: { nodes: d3Node[], links: d3Link[] } = {
  nodes: [
    {
      id: "1",
      label: "<h3>Node 1</h3>",
      labelType: "html"
    },
    {
      id: "2",
      label: "<h3>Node 2</h3>",
      labelType: "html",
      config: {
              style: 'fill: #afa'
          }
    }
  ],
  links: [
    {
      source: '1',
      target: '2',
      label: 'TO',
      config: {
              arrowheadStyle: 'display: none',
              curve: d3.curveBasis
      }
    },
  ]
}

export const ProblemGraph: ComponentType<ProblemGraphProps> =
    ({ problem }) =>
{
    return <div>
        <DagreGraph
            nodes={data.nodes}
            links={data.links}
            width='500'
            height='500'
            animate={1000}
            shape='circle'
            fitBoundaries
            zoomable
            onNodeClick={(e: any) => console.log(e)}
            onRelationshipClick={(e: any) => console.log(e)}
        />
    </div>;
};


interface GraphProps {
	nodes: d3Node[]
	links: d3Link[]
	zoomable?: boolean
	fitBoundaries?: boolean
	height?: string
	width?: string
	config?: GraphLabel
	animate?: number
	className?: string
	shape?: shapes
	onNodeClick?: Function
	onRelationshipClick?: Function
}
type shapes = 'rect' | 'circle' | 'ellipse'
type labelType = 'html' | 'svg' | 'string'

type Relationship = {
	v: any
	w: any
}

const DagreGraph: ComponentType<GraphProps> = ({
    nodes,
    links,
    zoomable,
    fitBoundaries,
    config,
    animate,
    shape,
    onNodeClick,
    onRelationshipClick,
    width,
    height,
    className
}) => {
	const svgElem = createRef<SVGSVGElement>()
	const innerG = createRef<SVGSVGElement>()

	const _getNodeData = (id: any) => {
		return nodes.find((node) => node.id === id)
	}

	const _drawChart = () => {
		let g = new dagreD3.graphlib.Graph().setGraph(config || {})

		nodes.forEach((node) =>
			g.setNode(node.id, {
				label: node.label,
				class: node.class || '',
				labelType: node.labelType || 'string',
				...node.config,
			})
		)

		if (shape) {
			g.nodes().forEach((v) => (g.node(v).shape = shape))
		}

		links.forEach((link) =>
			g.setEdge(link.source, link.target, { label: link.label || '', class: link.class || '', ...link.config })
		)

		let render = new dagreD3.render()
		let svg: any = d3.select(svgElem.current)
		let inner: any = d3.select(innerG.current)

		if (animate) {
			g.graph().transition = function transition(selection) {
				return selection.transition().duration(animate || 1000)
			}
		}

		render(inner, g)

		if (fitBoundaries) {
			//@BertCh recommendation for fitting boundaries
			const bounds = inner.node().getBBox()
			const parent = inner.node().parentElement || inner.node().parentNode
			const fullWidth = parent.clientWidth || parent.parentNode.clientWidth
			const fullHeight = parent.clientHeight || parent.parentNode.clientHeight
			const width = bounds.width
			const height = bounds.height
			const midX = bounds.x + width / 2
			const midY = bounds.y + height / 2
			if (width === 0 || height === 0) return // nothing to fit
			var scale = 0.9 / Math.max(width / fullWidth, height / fullHeight)
			var translate = [fullWidth / 2 - scale * midX, fullHeight / 2 - scale * midY]
			var transform = d3.zoomIdentity.translate(translate[0], translate[1]).scale(scale)

			svg
				.transition()
				.duration(animate || 0) // milliseconds
				.call(transform)
				// .call(zoom.transform, transform)
		}

		if (onNodeClick) {
			svg.selectAll('g.node').on('click', (id: any) => {
				let _node = g.node(id)
				let _original = _getNodeData(id)
				onNodeClick({ d3node: _node, original: _original })
			})
		}
		if (onRelationshipClick) {
			svg.selectAll('g.edgeLabel, g.edgePath').on('click', (id: Relationship) => {
				let _source = g.node(id.v)
				let _original_source = _getNodeData(id.v)

				let _target = g.node(id.w)
				let _original_target = _getNodeData(id.w)
				onRelationshipClick({
					d3source: _source,
					source: _original_source,
					d3target: _target,
					target: _original_target,
				})
			})
		}
	}

  return (
    <svg width={width} height={height} ref={svgElem} className={className || ''}>
      <g ref={innerG} />
    </svg>
  )
}

DagreGraph.defaultProps = {
  zoomable: false,
  fitBoundaries: false,
  className: 'dagre-d3-react',
}
