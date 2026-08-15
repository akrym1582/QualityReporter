import type {AxisScore,Config} from '../../model.js';import {axis} from '../quality-axis.js';
export const calculateArchitectureRisk=(components:{changeCoupling?:number;issues?:number},config:Config['qualityAxes']['architecture']):AxisScore=>config.enabled?axis(components,config.weights):axis({},{});
