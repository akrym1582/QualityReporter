import type {AxisScore,Config} from '../../model.js';import {axis} from '../quality-axis.js';
export const calculateKnowledgeRisk=(components:{ownershipConcentration?:number;authorDiversity?:number},config:Config['qualityAxes']['knowledge']):AxisScore=>config.enabled?axis(components,config.weights):axis({},{});
