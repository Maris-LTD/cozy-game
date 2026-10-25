using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolFreeStateTests : PoolingTestFixture
    {
        private readonly List<GameObject> _buffer = new List<GameObject>();

        private ObjectPoolService PrewarmedService(Transform root)
        {
            Config.SetPrewarmForTest(4, 0.25f);
            ObjectPoolService service = CreateRegisteredService(6, 4);
            service.BeginPrewarm(root);
            for (int i = 0; i < 100 && service.Phase != PoolServicePhase.Ready; i++)
            {
                service.StepPrewarm();
            }

            return service;
        }

        [Test]
        public void AC9_FreeObjects_HaveNoVisibleRendererAndNoSimulatingBody()
        {
            ObjectPoolService service = PrewarmedService(CreateRoot());
            int inspected = 0;

            for (int pool = 0; pool < 2; pool++)
            {
                service.CopyFreeObjects(PoolId(pool), _buffer);
                for (int i = 0; i < _buffer.Count; i++)
                {
                    Renderer[] renderers = _buffer[i].GetComponentsInChildren<Renderer>(true);
                    Rigidbody2D[] bodies = _buffer[i].GetComponentsInChildren<Rigidbody2D>(true);
                    Assert.AreEqual(3, renderers.Length);
                    Assert.AreEqual(1, bodies.Length);
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        Assert.IsFalse(renderers[r].enabled && renderers[r].gameObject.activeInHierarchy);
                    }

                    Assert.IsFalse(bodies[0].simulated && bodies[0].gameObject.activeInHierarchy);
                    inspected++;
                }
            }

            Assert.AreEqual(10, inspected);
        }

        [Test]
        public void AC9_FreeObjects_AreInactive_AndCountEqualsCapacity()
        {
            ObjectPoolService service = PrewarmedService(CreateRoot());

            Assert.AreEqual(6, service.CopyFreeObjects(PoolId(0), _buffer));
            for (int i = 0; i < _buffer.Count; i++)
            {
                Assert.IsFalse(_buffer[i].activeInHierarchy);
            }

            Assert.AreEqual(4, service.CopyFreeObjects(PoolId(1), _buffer));
        }

        [Test]
        public void FreeObjects_LiveUnderTheirOwnPoolContainerUnderRoot()
        {
            Transform root = CreateRoot();
            ObjectPoolService service = PrewarmedService(root);

            for (int pool = 0; pool < 2; pool++)
            {
                service.CopyFreeObjects(PoolId(pool), _buffer);
                for (int i = 0; i < _buffer.Count; i++)
                {
                    Transform container = _buffer[i].transform.parent;
                    Assert.AreEqual(PoolId(pool), container.name);
                    Assert.AreSame(root, container.parent);
                    Assert.IsTrue(container.gameObject.activeInHierarchy);
                }
            }

            Assert.AreEqual(2, root.childCount);
        }

        [Test]
        public void CopyFreeObjects_UnknownPool_ReturnsZero()
        {
            ObjectPoolService service = PrewarmedService(CreateRoot());

            Assert.AreEqual(0, service.CopyFreeObjects("missing", _buffer));
            Assert.AreEqual(0, service.CopyFreeObjects(null, _buffer));
        }
    }
}
