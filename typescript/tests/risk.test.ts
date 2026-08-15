import test from 'node:test';import assert from 'node:assert/strict';import {calculateRisk} from '../src/analysis/risk-calculator.js';import {defaults,type FileResult} from '../src/model.js';
const file=(path:string,n:number):FileResult=>({path,metrics:{loc:1,complexity:n,issues:{error:0,warning:0,info:0}},history:{commitCount:n,churn:n,authorCount:1,reworkRate:n/10},risk:{},couplings:[],functions:[]});test('risk percentile and missing coverage are normalized',()=>{const f=[file('a',1),file('b',9)];calculateRisk(f,defaults.riskWeights);assert.equal(f[0].risk.score,0);assert.equal(f[1].risk.score,94.1);assert.equal(f[1].risk.level,'critical');});

test('risk levels honor 40 60 and 80 boundaries',()=>{
  const files=Array.from({length:6},(_,i)=>file(String(i),i));
  calculateRisk(files,{change:1,churn:0,rework:0,complexity:0,coverage:0,issues:0});
  assert.deepEqual(files.map(x=>x.risk.score),[0,20,40,60,80,100]);
  assert.deepEqual(files.map(x=>x.risk.level),['low','low','medium','high','critical','critical']);
});

test('missing coverage is omitted and its weight is renormalized',()=>{
  const files=[file('low',1),file('high',2)];
  calculateRisk(files,{change:25,churn:0,rework:0,complexity:0,coverage:75,issues:0});
  assert.equal(files[1].risk.score,100);
  assert.equal(files[1].risk.coverageRiskPercentile,undefined);
});
